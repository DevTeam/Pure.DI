// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class LogInfoBuilder : IBuilder<LogEntry, LogInfo>
{
    public LogInfo Build(LogEntry logEntry)
    {
        var severityCode = logEntry.Severity switch
        {
            DiagnosticSeverity.Error => "ERR",
            DiagnosticSeverity.Warning => "WRN",
            DiagnosticSeverity.Info => "INF",
            DiagnosticSeverity.Hidden => "TRC",
            _ => throw new ArgumentOutOfRangeException(nameof(logEntry.Severity), logEntry.Severity, null)
        };

        DiagnosticDescriptor? descriptor = default;
        if (!string.IsNullOrWhiteSpace(logEntry.Id))
        {
            var message = new StringBuilder(logEntry.Message);
            if (logEntry.Exception is { } exception)
            {
                message.Append(", Message: \"");
                message.Append(exception.Message);
                message.Append("\", Stack Trace: \"");
                message.Append(exception.StackTrace.Replace(Environment.NewLine, " "));
                message.Append('"');
            }

            descriptor = new DiagnosticDescriptor(logEntry.Id!, severityCode, message.ToString(), severityCode, logEntry.Severity, true);
        }

        return new LogInfo(logEntry, descriptor);
    }
}