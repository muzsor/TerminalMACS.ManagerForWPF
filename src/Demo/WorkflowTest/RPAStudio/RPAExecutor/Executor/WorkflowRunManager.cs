﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RPA.Interfaces.Share;
using RPA.Services.Workflow;
using RPA.Shared.Executor;
using System;
using System.Activities;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPAExecutor.Executor
{
    public class WorkflowRunManager : IWorkflowManager
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<string, string> _configDict = new ConcurrentDictionary<string, string>();

        private WorkflowApplication _app;
        private string _name;
        private string _version;
        private string _mainXamlPath;
        private RPAExecutorAgent _agent;

        public WorkflowRunManager(RPAExecutorAgent agent, string name, string version, string mainXamlPath)
        {
            this._agent = agent;
            this._name = name;
            this._version = version;
            this._mainXamlPath = mainXamlPath;
        }

        public bool HasException { get; set; }//判断运行过程中是否发生了异常


        public static Dictionary<string, object> DeserializeArguments(DynamicActivity workflow, IDictionary<string, object> arguments)
        {
            if (workflow == null || workflow.Properties.Count == 0 || arguments == null)
            {
                return new Dictionary<string, object>();
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> keyValuePair in from a in arguments
                                                                  where a.Value != null
                                                                  select a)
            {
                string key = keyValuePair.Key;
                try
                {
                    if (workflow.Properties.Contains(key))
                    {
                        Type type = workflow.Properties[key].Type.GetGenericArguments()[0];
                        Type type2 = keyValuePair.Value.GetType();
                        if (type2 != type && !type.IsAssignableFrom(type2))
                        {
                            JToken jtoken = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(keyValuePair.Value));
                            object value = jtoken.ToObject(type);
                            dictionary.Add(key, value);
                        }
                        else
                        {
                            dictionary.Add(key, keyValuePair.Value);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return dictionary;
        }

        public void Run()
        {
            HasException = false;

            Activity workflow = ActivityXamlServices.Load(_mainXamlPath);

            try
            {
                var result = ActivityValidationServices.Validate(workflow);
                if (result.Errors.Count == 0)
                {
                    _logger.Debug(string.Format("开始执行工作流文件 {0} ……", _mainXamlPath));

                    if (_app != null)
                    {
                        _app.Terminate("");
                    }

                    //"{'inArg':'value','integer':3}"
                    var input_args = GetConfig("input_args");

                    IDictionary<string, object> inputs = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(input_args))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        };

                        inputs = JsonConvert.DeserializeObject<IDictionary<string, object>>(input_args, settings);

                        inputs = DeserializeArguments(workflow as DynamicActivity, inputs);
                    }

                    _app = new WorkflowApplication(workflow, inputs);
                    _app.Extensions.Add(new LogToOutputWindowTextWriter());

                    if (workflow is DynamicActivity)
                    {
                        var wr = new WorkflowRuntime();
                        wr.RootActivity = workflow;
                        _app.Extensions.Add(wr);
                    }

                    _app.OnUnhandledException = WorkflowApplicationOnUnhandledException;
                    _app.Completed = WorkflowApplicationExecutionCompleted;
                    _app.Run();
                }
                else
                {
                    _logger.Debug(string.Format("工作流校验错误，请检查参数配置： {0} ……", _mainXamlPath));
                    _agent.OnExecutionCompleted(true);
                }
            }
            catch (Exception err)
            {
                _logger.Error(err);
                _agent.OnExecutionCompleted(true);
            }

        }


        /// <summary>
        /// 停止工作流运行
        /// </summary>
        public void Stop()
        {
            if (_app != null)
            {
                try
                {
                    _app.Terminate("执行由用户主动停止", new TimeSpan(0, 0, 0, 5));
                }
                catch (Exception err)
                {
                    _logger.Error(err);
                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="e">参数</param>
        /// <returns>异常对象</returns>
        private UnhandledExceptionAction WorkflowApplicationOnUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs e)
        {
            HasException = true;

            var name = e.ExceptionSource.DisplayName;
            SharedObject.Instance.Output(SharedObject.enOutputType.Error, string.Format("{0} 执行时出现异常", name), e.UnhandledException.ToString());

            return UnhandledExceptionAction.Terminate;
        }


        /// <summary>
        /// 工作流执行完成
        /// </summary>
        /// <param name="obj">对象</param>
        private void WorkflowApplicationExecutionCompleted(WorkflowApplicationCompletedEventArgs obj)
        {
            if (obj.TerminationException != null)
            {
                if (!string.IsNullOrEmpty(obj.TerminationException.Message))
                {
                    HasException = true;

                    _agent.OnException(obj.TerminationException.Message, obj.TerminationException.ToString());
                }
            }

            _agent.OnExecutionCompleted(HasException);

            _logger.Debug(string.Format("结束执行工作流文件 {0}", _mainXamlPath));
        }

        public void RedirectLogToOutputWindow(SharedObject.enOutputType type, string msg, string msgDetails)
        {
            _agent.RedirectLogToOutputWindow(type.ToString(), msg, msgDetails);
        }

        public string GetConfig(string key)
        {
            if (_configDict.ContainsKey(key))
                return _configDict[key];
            else
                return "";
        }

        public void SetConfig(string key, string val)
        {
            _logger.Debug($"SetConfig,key={key},val={val}");
            _configDict[key] = val;
        }

        public void OnUnhandledException(string title, Exception err)
        {
            _agent.OnException(title, err.ToString());
            _agent.OnExecutionCompleted(true);
        }

        public void RedirectNotification(string notification, string notificationDetails)
        {
            _agent.RedirectNotification(notification, notificationDetails);
        }
    }
}
