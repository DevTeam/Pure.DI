// ReSharper disable ArrangeNamespaceBody
// ReSharper disable RedundantUsingDirective
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;

    public class ServiceCollectionsWithArgs
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=10
        // $description=Service collection with arguments
        // {
        [Fact]
        public void Run()
        {
            DI.Setup("MyComposerWithArgs")
                .Arg<string>()
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>();
            
            var serviceProvider =
                // Creates some serviceCollection
                new ServiceCollection()
                // Adds some registrations with any lifetime
                .AddScoped<ServiceConsumer>()
                // Adds registrations produced by Pure DI above
                .AddMyComposerWithArgs("Abc")
                // Builds a service provider
                .BuildServiceProvider();

            var consumer = serviceProvider.GetRequiredService<ServiceConsumer>();
            var instance = serviceProvider.GetRequiredService<IService>();
            consumer.Service.Dependency.ShouldBe(instance.Dependency);
            consumer.Service.ShouldNotBe(instance);
            consumer.Setting.ShouldBe("Abc");
        }

        public class ServiceConsumer
        {
            public ServiceConsumer(IService service, string setting)
            {
                Service = service;
                Setting = setting;
            }

            public IService Service { get; }

            public string Setting { get; }
        }
        // }
    }
}
