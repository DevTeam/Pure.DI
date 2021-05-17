namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class Autowiring
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=01
            // $description=Autowiring
            // $header=Auto-wring is the most natural way to use containers. In the first step, we should create a container. At the second step, we bind interfaces to their implementations. After that, the container is ready to resolve dependencies.
            // {
            // Create the container and configure it, using full autowiring
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolve an instance of interface `IService`
            var instance = AutowiringDI.Resolve<IService>();
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
