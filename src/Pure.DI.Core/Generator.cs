namespace Pure.DI;

public static class Generator
{
    private static readonly Composition Composition = new();

    public static IEnumerable<Source> GetApi(CancellationToken cancellationToken) => 
        Composition.ApiBuilder.Build(Unit.Shared);

    public static IDisposable RegisterObserver<T>(IObserver<T> observer) => 
        Composition.ObserversRegistry.Register(observer);

    public static void Generate(IOptions options,
        ISourcesRegistry sources,
        IDiagnostic diagnostic,
        IEnumerable<SyntaxUpdate> updates,
        CancellationToken cancellationToken) =>
        Composition.CreateGenerator(
            options: options,
            sources: sources,
            diagnostic: diagnostic,
            cancellationToken: cancellationToken)
            .Build(updates);
}