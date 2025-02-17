﻿// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvocationIsSkipped
namespace Clock.Models;

using System.Diagnostics;

class Log<T>(IClock clock) : ILog<T>
{
    public void Info(string message) =>
        Debug.WriteLine($"{clock.Now:HH:mm:ss.fff} {typeof(T).Name,-32} {message}");
}