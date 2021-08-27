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
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolve an instance of interface `IService`
            var instance = AutowiringDI.Resolve<IService>();
            // }
            instance.ShouldBeOfType<Service>();
        }
    }
}
