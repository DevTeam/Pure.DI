namespace Pure.DI;

using Core;

public class Facade
{
    public static IEnumerable<Source> GetApi(in CancellationToken cancellationToken) => 
        Composition.ResolveIBuilderUnitIEnumerableSource().Build(Unit.Shared, cancellationToken);

    public static Facade Create(IContextOptions options, IContextProducer producer, IContextDiagnostic diagnostic) => 
        Composition.Resolve<Facade>(options, producer, diagnostic);

    internal Facade(
        IObserversRegistry observersRegistry,
        IGenerator generator)
    {
        ObserversRegistry = observersRegistry;
        Generator = generator;
    }
    
    internal IObserversRegistry ObserversRegistry { get; }

    public IGenerator Generator { get; }
}