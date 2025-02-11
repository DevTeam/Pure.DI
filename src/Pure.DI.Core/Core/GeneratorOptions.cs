namespace Pure.DI.Core;

internal sealed class GeneratorOptions(
    ParseOptions parseOptions,
    AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    : IGeneratorOptions
{
    public ParseOptions ParseOptions { get; } = parseOptions;

    public AnalyzerConfigOptions GlobalOptions => analyzerConfigOptionsProvider.GlobalOptions;

    public AnalyzerConfigOptions GetOptions(SyntaxTree tree) => analyzerConfigOptionsProvider.GetOptions(tree);
}