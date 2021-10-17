namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class ResolvingByTypeArgument
    {
        [Fact]
        public void Run()
        {
            // $visible=false
            // $tag=1 Basics
            // $priority=01
            // $description=Resolving by a type argument
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolves an instance of interface `IService`
            var instance = ResolvingByTypeArgumentDI.Resolve(typeof(IService));
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
