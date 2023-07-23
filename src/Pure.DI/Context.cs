namespace Pure.DI;

internal class Context: IContextOptions, IContextProducer, IContextDiagnostic
{
    private readonly SourceProductionContext _sourceProductionContext;
    private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

    public Context(
        SourceProductionContext sourceProductionContext,
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    {
        _sourceProductionContext = sourceProductionContext;
        ParseOptions = parseOptions;
        _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
    }
    
    public ParseOptions ParseOptions { get; }

    public AnalyzerConfigOptions GlobalOptions => _analyzerConfigOptionsProvider.GlobalOptions;

    public AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _analyzerConfigOptionsProvider.GetOptions(tree);

    public void AddSource(string hintName, SourceText sourceText) => _sourceProductionContext.AddSource(hintName, sourceText);

    public void ReportDiagnostic(Diagnostic diagnostic) => _sourceProductionContext.ReportDiagnostic(diagnostic);
}