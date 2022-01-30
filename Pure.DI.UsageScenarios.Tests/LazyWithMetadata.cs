namespace Pure.DI.UsageScenarios.Tests;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class LazyWithMetadata
{
    [Fact]
    public void Run()
    {
        // $visible=true
        // $tag=3 BCL types
        // $priority=01
        // $description=Lazy with metadata
        // $header=_Lazy_ dependency helps when a logic needs to inject _Lazy<T, TMetadata>_ to get instance once on-demand and the metadata associated with the referenced object.
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // {
        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<IService<TT>>().To<Service<TT>>()
            .Bind<CompositionRoot<Lazy<IService, IService<int>>>>().To<CompositionRoot<Lazy<IService, IService<int>>>>();

        // Resolve the instance of Lazy<IService> with some metadata, for instance of type IService<int>
        var lazy = LazyWithMetadataDI.Resolve<CompositionRoot<Lazy<IService, IService<int>>>>().Root;

        // Get the instance via Lazy
        var instance = lazy.Value;
        // }
        // Check the instance
        instance.ShouldBeOfType<Service>();
        lazy.Metadata.ShouldBeOfType<Service<int>>();
    }
}