// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class LogInfoBuilder : IFastBuilder<LogEntry, LogInfo>
{
    public LogInfo Build(in LogEntry logEntry)
    {
        var severityCode = logEntry.Severity switch
        {
            DiagnosticSeverity.Error => "ERR",
            DiagnosticSeverity.Warning => "WRN",
            DiagnosticSeverity.Info => "INF",
            DiagnosticSeverity.Hidden => "TRC",
            _ => throw new ArgumentOutOfRangeException(nameof(logEntry.Severity), logEntry.Severity, null)
        };

        DiagnosticDescriptor? descriptor = null;
        ImmutableDictionary<string, string?>? properties = null;
        if (logEntry.Id is not null && !string.IsNullOrWhiteSpace(logEntry.Id))
        {
            var message = new StringBuilder(logEntry.Message);
            if (logEntry.Exception is {} exception)
            {
                message.Append(", Message: \"");
                message.Append(exception.Message);
                message.Append("\", Stack Trace: \"");
                message.Append(exception.StackTrace.Replace(Environment.NewLine, " "));
                message.Append('"');
            }

            var category = LogMetadata.GetCategory(logEntry.Id);
            var description = LogMetadata.GetDescription(logEntry.Id);
            var helpLink = LogMetadata.GetHelpLink(logEntry.Id);
            descriptor = new DiagnosticDescriptor(
                logEntry.Id,
                severityCode,
                message.ToString(),
                category,
                logEntry.Severity,
                true,
                description: description,
                helpLinkUri: helpLink);
            if (!string.IsNullOrWhiteSpace(logEntry.MessageKey))
            {
                properties = ImmutableDictionary<string, string?>
                    .Empty
                    .Add("puredi.messageKey", logEntry.MessageKey);
            }
        }

        return new LogInfo(logEntry, descriptor, properties);
    }
}
