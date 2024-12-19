// ReSharper disable UnusedTypeParameter

namespace Pure.DI.Core;

internal interface ILogger
{
    void Log(in LogEntry logEntry);
}