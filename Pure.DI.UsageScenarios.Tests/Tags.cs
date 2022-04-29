// ReSharper disable ArrangeNamespaceBody
// ReSharper disable RedundantUsingDirective
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
        // $visible=true
        // $tag=1 Basics
        // $priority=01
        // $description=Tags
        // $header=Tags are useful while binding to several implementations of the same abstract types.
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Adds some tags for a specific contract
                .Bind<IService>("Service 1").To<Service>()
                // Adds some tags for a binding
                .Bind<IService>().Tags("Service 2", 2).As(Lifetime.Singleton).To<ServiceRecord>()
                .Bind<Consumer>().To<Consumer>();

            var consumer = TagsDI.Resolve<Consumer>();
            consumer.Service1.ShouldBeOfType<Service>();
            consumer.Service2.ShouldBeOfType<ServiceRecord>();
            consumer.Service3.ShouldBe(consumer.Service2);
        }

        internal class Consumer
        {
            public Consumer(
                [Tag("Service 1")] IService service1,
                [Tag("Service 2")] IService service2,
                [Tag(2)] IService service3)
            {
                Service1 = service1;
                Service2 = service2;
                Service3 = service3;
            }
            
            public IService Service1 { get; }

            public IService Service2 { get; }

            public IService Service3 { get; }
        }
        // }
    }
}
