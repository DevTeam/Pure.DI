// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvocationIsSkipped
namespace Clock.Models;

using System.Diagnostics;

internal class Log<T> : ILog<T>
{
    public void Info(string message) =>
        Debug.WriteLine($"{typeof(T).Name} {message}");
}