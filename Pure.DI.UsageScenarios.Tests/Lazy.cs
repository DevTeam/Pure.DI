namespace Pure.DI.UsageScenarios.Tests;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Lazy
{
    [Fact]
    public void Run()
    {
        // $visible=true
        // $tag=3 BCL types
        // $priority=01
        // $description=Lazy
        // $header=_Lazy_ dependency helps when a logic needs to inject _Lazy<T>_ to get instance once on-demand.
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // {
        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<CompositionRoot<Lazy<IService>>>().To<CompositionRoot<Lazy<IService>>>();

        // Resolve the instance of Lazy<IService>
        var lazy = LazyDI.Resolve<CompositionRoot<Lazy<IService>>>().Root;

        // Get the instance via Lazy
        var instance = lazy.Value;
        // }
        // Check the instance
        instance.ShouldBeOfType<Service>();
    }
}