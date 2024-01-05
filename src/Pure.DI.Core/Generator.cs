namespace Pure.DI;

public sealed partial class Generator
{
    public void Generate(
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        in SourceProductionContext sourceProductionContext,
        in ImmutableArray<GeneratorSyntaxContext> changes,
        CancellationToken cancellationToken)
    {
        if (changes.IsEmpty)
        {
            return;
        }

        Generate(
            new GeneratorOptions(parseOptions, analyzerConfigOptionsProvider),
            new GeneratorSources(sourceProductionContext),
            new GeneratorDiagnostic(sourceProductionContext),
            changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel)),
            cancellationToken);
    }

    internal void Generate(
        IGeneratorOptions options,
        IGeneratorSources sources,
        IGeneratorDiagnostic diagnostic,
        IEnumerable<SyntaxUpdate> updates,
        CancellationToken cancellationToken) =>
        CreateGenerator(
                options: options,
                sources: sources,
                diagnostic: diagnostic,
                cancellationToken: cancellationToken)
            .Build(updates);
}