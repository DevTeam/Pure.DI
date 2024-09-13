// ReSharper disable UnusedTypeParameter

namespace Pure.DI.Core;

internal interface ILogger<T>
{
    void Log(in LogEntry logEntry);
}