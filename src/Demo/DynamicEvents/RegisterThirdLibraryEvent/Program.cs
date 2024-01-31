﻿using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;

// 加载第三方库  
var assembly = Assembly.LoadFrom("ThirdLibrary.dll");

// 创建TestClass的实例  
var testClassType = assembly.GetType("ThirdLibrary.TestClass");
var testClassInstance = Activator.CreateInstance(testClassType);
var fields = testClassType.GetFields();

// 1|获取NoParamEvent委托  
var noParamEventField = fields.First(field => "NoParamEvent" == field.Name);
var noParamEventActionType = typeof(Action);
var noParamEventHandler = new Action(EventHandlerMethod);
noParamEventField.SetValue(testClassInstance, noParamEventHandler);

// 2|获取OneParamEvent委托，并设置事件参数处理程序  
var oneParamEventField = fields.First(field => "OneParamEvent" == field.Name);
var oneParamEventActionType = typeof(Action<>).MakeGenericType(typeof(string));
var oneParamEventHandler = new Action<string>(OneParamEventHandler);
oneParamEventField.SetValue(testClassInstance, oneParamEventHandler);

// 3|获取TwoParamEvent委托，并设置事件参数处理程序  
var twoParamEventField = fields.First(field => "TwoParamEvent" == field.Name);
var twoParamEventHandler = new Action<string, dynamic>(TwoParamEventHandler); // 使用dynamic作为第二个参数的类型  
twoParamEventField.SetValue(testClassInstance, twoParamEventHandler);

// 4.1|EventHandler
//var eventHandlerEventField = fields.First(field => "EventHandlerEvent" == field.Name);
//var eventHandlerMethod = new Action<object, dynamic>(EventHandlerEventHandler);  
//twoParamEventField.SetValue(testClassInstance, twoParamEventHandler);

var events = testClassType.GetEvents();

// 4.2|获取EventHandler事件
var eventHandlerEventField = events.First(item => "EventHandlerEvent" == item.Name);
var eventHandlerType = eventHandlerEventField.EventHandlerType;
var eventHandlerMethod = new EventHandler<dynamic>(EventHandlerEventHandler);
var handle = Delegate.CreateDelegate(eventHandlerType, eventHandlerMethod.Method);
eventHandlerEventField.AddEventHandler(null, handle);

// 模拟触发事件通知
var callEventMethod = testClassType.GetMethods().First(method => "CallTwoParamEvent" == method.Name);
callEventMethod.Invoke(testClassInstance, null);

// NoParamEvent事件处理程序方法  
void EventHandlerMethod()
{
    Console.WriteLine("NoParamEvent event raised.");
}

// OneParamEvent事件处理程序方法，需要一个字符串参数  
void OneParamEventHandler(string param)
{
    Console.WriteLine($"OneParamEvent event raised with parameter: {param}");
}

// TwoParamEvent事件处理程序方法，需要两个参数：string和EventParam类型（通过反射传递，EventParam类型使用动态类型dynamic替换）  
void TwoParamEventHandler(string param1, dynamic param2) // 使用dynamic作为第二个参数的类型，并通过反射传递实际参数值  
{
    Console.WriteLine($"param1={param1}, param2.Param1={param2.Message}");
}

void EventHandlerEventHandler(object sender, dynamic param)
{
   Console.WriteLine($"param.Param1={param.Message}");
}