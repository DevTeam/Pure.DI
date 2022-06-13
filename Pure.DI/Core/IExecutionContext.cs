namespace Pure.DI.Core;

internal interface IExecutionContext
{
    Compilation Compilation { get; }
    
    ParseOptions ParseOptions { get; }

    CancellationToken CancellationToken { get; }

    bool TryGetOption(string optionName, out string value);

    void AddSource(string hintName, SourceText sourceText);

    public void ReportDiagnostic(Diagnostic diagnostic);
}