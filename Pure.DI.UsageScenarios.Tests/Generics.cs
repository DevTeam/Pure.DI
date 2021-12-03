// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageScenarios.Tests
{
    using Xunit;

    public class Generics
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=01
        // $description=Generics
        // $header=Auto-wring of generic types via binding of open generic types or generic type markers are working the same way.
        // $footer=Open generic type instance, for instance, like IService&lt;TT&gt; here, cannot be a composition root instance. This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // $footer=The actual composition for the example above looks like this:
        // $footer=```CSharp
        // $footer=new Consumer(new Service<int>(Dependency()));
        // $footer=```
        // {
        public class Consumer
        {
            public Consumer(IService<int> service) { }
        }

        // }
        [Fact]
        public void Run()
        {
            // {
        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            // Bind a generic type
            .Bind<IService<TT>>().To<Service<TT>>()
            .Bind<Consumer>().To<Consumer>();

        var instance = GenericsDI.Resolve<Consumer>();
            // }
        }
    }
}
