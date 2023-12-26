namespace Pure.DI.Core;

internal class GeneratorOptions(
    ParseOptions parseOptions,
    AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    : IGeneratorOptions
{
    public ParseOptions ParseOptions { get; } = parseOptions;

    public AnalyzerConfigOptions GlobalOptions => analyzerConfigOptionsProvider.GlobalOptions;

    public AnalyzerConfigOptions GetOptions(SyntaxTree tree) => analyzerConfigOptionsProvider.GetOptions(tree);
}