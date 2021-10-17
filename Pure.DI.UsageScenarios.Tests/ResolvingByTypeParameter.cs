namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class ResolvingByTypeParameter
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=01
            // $description=Resolving by a type parameter
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs) as many others.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolves an instance of interface `IService`
            var instance = ResolvingByTypeParameterDI.Resolve<IService>();
            // }
            instance.ShouldBeOfType<Service>();
        }
    }
}
