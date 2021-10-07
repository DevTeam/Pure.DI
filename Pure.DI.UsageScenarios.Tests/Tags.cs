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
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind using several tags
                .Bind<IService>(10, 'a').Tags("abc", 99).To<Service>();

            // Resolve instances using tags
            var instance1 = TagsDI.Resolve<IService>(10);
            var instance2 = TagsDI.Resolve<IService>('a');
            var instance3 = TagsDI.Resolve<IService>("abc");
            var instance4 = TagsDI.Resolve<IService>(99);
            // }
            // Check instances
            // Resolve the instance using the empty tag
            var instance5 = Should.Throw<ArgumentException>(TagsDI.Resolve<Service>);
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
            instance3.ShouldBeOfType<Service>();
            instance4.ShouldBeOfType<Service>();
        }
    }
}
