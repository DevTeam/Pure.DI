namespace Pure.DI.Core;

internal class GeneratorOptions : IGeneratorOptions
{
    private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

    public GeneratorOptions(
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    {
        _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
        ParseOptions = parseOptions;
    }

    public ParseOptions ParseOptions { get; }

    public AnalyzerConfigOptions GlobalOptions => _analyzerConfigOptionsProvider.GlobalOptions;

    public AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _analyzerConfigOptionsProvider.GetOptions(tree);
}