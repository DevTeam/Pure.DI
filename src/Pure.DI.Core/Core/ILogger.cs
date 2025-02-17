// ReSharper disable UnusedTypeParameter

namespace Pure.DI.Core;

interface ILogger
{
    void Log(in LogEntry logEntry);
}