namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class Bindings
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=01
            // $description=Bindings
            // $header=It is possible to bind any number of contracts to an implementation.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind using few types
                .Bind<IService>().Bind<IAnotherService>().Tag("abc").To<Service>();

            // Resolve instances using different types
            var instance1 = BindingsDI.Resolve<IService>("abc");
            var instance2 = BindingsDI.Resolve<IAnotherService>("abc");
            // }
            // Check instances
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
        }
    }
}
