namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class CompilationDiagnostic : IDiagnostic
    {
        private readonly ILog<CompilationDiagnostic> _log;

        public CompilationDiagnostic(ILog<CompilationDiagnostic> log) => _log = log;

        public GeneratorExecutionContext Context { get; set; }

        public void Error(string id, string message, Location? location = null)
        {
            Context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Error",
                    message,
                    "Error",
                    DiagnosticSeverity.Error,
                    true),
                location));

            if (id != Diagnostics.Unhandled)
            {
                _log.Trace(() => new []{ $"{id} {message}"});
            }
        }

        public void Warning(string id, string message, Location? location = null)
        {
            Context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Warning",
                    message,
                    "Warning",
                    DiagnosticSeverity.Warning,
                    true),
                location));

            _log.Trace(() => new []{ $"{id} {message}" });
        }

        public void Information(string id, string message, Location? location = null)
        {
            Context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Info",
                    message,
                    "Info",
                    DiagnosticSeverity.Info,
                    true),
                location));

            _log.Trace(() => new []{ $"{id} {message}" });
        }
    }
}
