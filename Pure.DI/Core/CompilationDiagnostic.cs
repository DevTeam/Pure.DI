namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class CompilationDiagnostic : IDiagnostic
{
    private readonly ILog<CompilationDiagnostic> _log;

    public CompilationDiagnostic(ILog<CompilationDiagnostic> log) => _log = log;

    public IExecutionContext? Context { get; set; }

    public void Error(string id, string message, Location? location = null)
    {
        try
        {
            Context?.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Error",
                    message,
                    "Error",
                    DiagnosticSeverity.Error,
                    true),
                location));
        }
        catch
        {
            // ignored
        }

        if (id != Diagnostics.Error.Unhandled)
        {
            _log.Trace(() => new[]
            {
                $"{id} {message}"
            });
        }
    }

    public void Warning(string id, string message, Location? location = null)
    {
        try
        {
            Context?.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Warning",
                    message,
                    "Warning",
                    DiagnosticSeverity.Warning,
                    true),
                location));
        }
        catch
        {
            // ignored
        }

        _log.Trace(() => new[]
        {
            $"{id} {message}"
        });
    }

    public void Information(string id, string message, Location? location = null)
    {
        try
        {
            Context?.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Info",
                    message,
                    "Info",
                    DiagnosticSeverity.Info,
                    true),
                location));
        }
        catch
        {
            // ignored
        }

        _log.Trace(() => new[]
        {
            $"{id} {message}"
        });
    }
}