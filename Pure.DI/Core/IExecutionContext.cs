namespace Pure.DI.Core;

internal interface IExecutionContext
{
    Compilation Compilation { get; }

    CancellationToken CancellationToken { get; }

    void AddSource(string hintName, SourceText sourceText);

    public void ReportDiagnostic(Diagnostic diagnostic);
}