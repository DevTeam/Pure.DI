namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class CompilationDiagnostic : IDiagnostic
    {
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

            throw new HandledException(message);
        }

        public void Warning(string id, string message, Location? location = null) =>
            Context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Warning",
                    message,
                    "Warning",
                    DiagnosticSeverity.Warning,
                    true),
                location));

        public void Information(string id, string message, Location? location = null) =>
            Context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    "Info",
                    message,
                    "Info",
                    DiagnosticSeverity.Info,
                    true),
                location));
    }
}
