namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "UnusedVariable")]
    public class TagsWithType
    {
        [Fact]
        public void Run()
        {
            // $visible=false
            // $tag=1 Basics
            // $priority=01
            // $description=Tags
            // $header=Tags are useful while binding to several implementations of the same abstract types.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind using several tags
                .Bind<IService>().Tag(10).Tag("abc").To<Service>()
                .Bind<IService>().To<Service>();

            // Resolve instances using tags
            var instance1 = (IService)TagsWithTypeDI.Resolve(typeof(IService), "abc");
            var instance2 = (IService)TagsWithTypeDI.Resolve(typeof(IService), 10);

            // Resolve the instance using the empty tag
            var instance3 = (IService)TagsWithTypeDI.Resolve(typeof(IService));
            // }
            // Check instances
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
            instance3.ShouldBeOfType<Service>();
        }
    }
}
