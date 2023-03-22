namespace Pure.DI;

using Core;

public static class Facade
{
    internal static IObserversRegistry ObserversRegistry => Composition.ResolveIObserversRegistry(); 
    
    public static IEnumerable<Source> GetApi(in CancellationToken cancellationToken)
    {
        return Composition.ResolveIBuilderUnitIEnumerableSource().Build(Unit.Shared, cancellationToken);
    }

    public static IGenerator GetGenerator(IContextOptions options, IContextProducer producer, IContextDiagnostic diagnostic)
    {
        Composition.ResolveIContextInitializer().Initialize(options, producer, diagnostic);
        return Composition.ResolveIGenerator();
    }
}