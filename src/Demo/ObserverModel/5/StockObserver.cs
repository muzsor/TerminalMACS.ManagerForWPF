﻿using System;

namespace ObserverModel._5;

/// <summary>
///     看股票的同事
/// </summary>
internal class StockObserver
{
    private readonly string name;
    private readonly Subject sub;

    public StockObserver(string name, Subject sub)
    {
        this.name = name;
        this.sub = sub;
    }

    // 方法“更新”名改为“关闭股票程序”
    // 关闭股票行情
    public void CloseStockMarket()
    {
        Console.WriteLine($"{sub.SubjectState}{name}关闭股票行情，继续工作! ");
    }
}