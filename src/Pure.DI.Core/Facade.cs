namespace Pure.DI;

using Core;

public static class Facade
{
    internal static IObserversRegistry ObserversRegistry => Composer.ResolveIObserversRegistry(); 
    
    public static IEnumerable<Source> GetApi(in CancellationToken cancellationToken)
    {
        return Composer.ResolveIBuilderUnitIEnumerableSource().Build(Unit.Shared, cancellationToken);
    }

    public static IGenerator GetGenerator(IContextOptions options, IContextProducer producer, IContextDiagnostic diagnostic)
    {
        Composer.ResolveIContextInitializer().Initialize(options, producer, diagnostic);
        return Composer.ResolveIGenerator();
    }
}