namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class SingletonLifetime
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=2 Lifetimes
            // $priority=01
            // $description=Singleton lifetime
            // $header=[Singleton](https://en.wikipedia.org/wiki/Singleton_pattern) is a design pattern that supposes for having only one instance of some class during the whole application lifetime. The main complaint about Singleton is that it contradicts the Dependency Injection principle and thus hinders testability. It essentially acts as a global constant, and it is hard to substitute it with a test when needed. The _Singleton lifetime_ is indispensable in this case.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Use the Singleton lifetime
                .Bind<IService>().As(Singleton).To<Service>();

            // Resolve the singleton twice
            var instance1 = SingletonLifetimeDI.Resolve<IService>();
            var instance2 = SingletonLifetimeDI.Resolve<IService>();

            // Check that instances are equal
            instance1.ShouldBe(instance2);
            // }
        }
    }
}
