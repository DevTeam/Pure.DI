namespace Pure.DI.UsageScenarios.Tests;

public class CompositionRoot
{
    [Fact]
    public void Run()
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=00
        // $description=Composition Root
        // $header=This sample demonstrates the most efficient way of getting a composition root object, free from any impact on memory consumption and performance.
        // $footer=Actually, the method _ResolveIService_ looks like this:
        // $footer=```csharp
        // $footer=[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        // $footer=public static IService ResolveIService() => new Service(new Dependency());
        // $footer=```
        // $footer=and the compiler just inserts this set of constructor calls instead of ```Composer.ResolveIService()```:
        // $footer=```csharp
        // $footer=new Service(new Dependency())
        // $footer=```
        // {
        DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>();

        // Resolves an instance of interface `IService`
        var instance = Composer.ResolveIService();
        // }
        instance.ShouldBeOfType<Service>();
    }
}