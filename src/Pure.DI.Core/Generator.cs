namespace Pure.DI;

public class Generator
{
    private static readonly CompositionBase CompositionBase = new();
    private readonly Composition _composition;

    public static IEnumerable<Source> GetApi(CancellationToken cancellationToken) => 
        CompositionBase.ApiBuilder.Build(Unit.Shared, cancellationToken);

    public Generator(IOptions options, ISourcesRegistry sources, IDiagnostic diagnostic) =>
        _composition = new Composition(options: options, sources: sources, diagnostic: diagnostic);

    public void Generate(
        IEnumerable<SyntaxUpdate> updates,
        CancellationToken cancellationToken) =>
        _composition.Generator.Build(updates, cancellationToken);

    public IDisposable RegisterObserver<T>(IObserver<T> observer) => 
        _composition.ObserversRegistry.Register(observer);
}