namespace Pure.DI;

public sealed partial class Generator
{
    public void Generate(
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        in SourceProductionContext sourceProductionContext,
        in ImmutableArray<GeneratorSyntaxContext> changes,
        CancellationToken cancellationToken) => 
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Generate(
            new GeneratorOptions(parseOptions, analyzerConfigOptionsProvider),
            new GeneratorSources(sourceProductionContext),
            new GeneratorDiagnostic(sourceProductionContext),
            changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel)).ToImmutableArray(),
            cancellationToken);
}