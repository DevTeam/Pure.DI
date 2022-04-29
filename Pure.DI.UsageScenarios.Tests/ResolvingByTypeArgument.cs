// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using static Lifetime;

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
                .Bind<IService>().To<Service>()
                .Bind<IService>("Some tag").As(PerResolve).To<Service>();

            // Resolves an instance of interface `IService`
            var instance = ResolvingByTypeArgumentDI.Resolve(typeof(IService));
            var taggedInstance = ResolvingByTypeArgumentDI.Resolve(typeof(IService), "Some tag");
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
            taggedInstance.ShouldBeOfType<Service>();
        }
    }
}
