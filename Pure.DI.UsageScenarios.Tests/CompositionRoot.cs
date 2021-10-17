namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class CompositionRoot
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=00
            // $description=Composition Root
            // $header=This sample demonstrates the most efficient way of getting a composition root object, free from any impact on memory consumption and performance. Each ordinary binding has its method to resolve a related instance as a composition root object.
            // {
            DI.Setup("Composer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolves an instance of interface `IService` using a particular method generated for each ordinary binding
            var instance = Composer.ResolveIService();
            // }
            instance.ShouldBeOfType<Service>();
        }
    }
}
