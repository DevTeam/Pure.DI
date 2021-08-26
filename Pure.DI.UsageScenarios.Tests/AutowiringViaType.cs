namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class AutowiringViaType
    {
        [Fact]
        public void Run()
        {
            // $visible=false
            // $tag=1 Basics
            // $priority=01
            // $description=Autowiring
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolve an instance of interface `IService`
            var instance = AutowiringViaTypeDI.Resolve(typeof(IService));
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
