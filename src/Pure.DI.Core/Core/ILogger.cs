// ReSharper disable UnusedTypeParameter
namespace Pure.DI.Core;

internal interface ILogger<T>
{
    bool IsEnabled(DiagnosticSeverity severity);
    
    void Log(in LogEntry logEntry);
}