namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "UnusedVariable")]
    public class Tags
    {
        [Fact]
        public void Run()
        {
            // $visible=true
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
            var instance1 = TagsDI.Resolve<IService>("abc");
            var instance2 = TagsDI.Resolve<IService>(10);

            // Resolve the instance using the empty tag
            var instance3 = TagsDI.Resolve<IService>();
            // }
            // Check instances
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
            instance3.ShouldBeOfType<Service>();
        }
    }
}
