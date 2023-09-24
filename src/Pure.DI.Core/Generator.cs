namespace Pure.DI;

public sealed class Generator
{
    private readonly Composition _composition = new();

    public IEnumerable<Source> GetApi() => 
        _composition.ApiBuilder.Build(Unit.Shared);

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

    internal IDisposable RegisterObserver<T>(IObserver<T> observer) => 
        _composition.ObserversRegistry.Register(observer);

    internal void Generate(
        IGeneratorOptions options,
        IGeneratorSources sources,
        IGeneratorDiagnostic diagnostic,
        IEnumerable<SyntaxUpdate> updates,
        CancellationToken cancellationToken) =>
        _composition.CreateGenerator(
                options: options,
                sources: sources,
                diagnostic: diagnostic,
                cancellationToken: cancellationToken)
            .Build(updates);
}