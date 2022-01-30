namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;

    public class ServiceCollections
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=01
        // $description=Service collection
        // $header=In the cases when a project references the Microsoft Dependency Injection library, an extension method for ```IServiceCollection``` is generating automatically with a name like _Add..._ plus the name of a generated class, here it is ```AddMyComposer()``` for class ```public class MyComposer { }```.
        // {
        [Fact]
        public void Run()
        {
            DI.Setup("MyComposer")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>();
            
            var serviceProvider =
                // Creates some serviceCollection
                new ServiceCollection()
                    // Adds some registrations with any lifetime
                    .AddScoped<ServiceConsumer>()
                // Adds registrations produced by Pure DI above
                .AddMyComposer()
                // Builds a service provider
                .BuildServiceProvider();
            
            var consumer = serviceProvider.GetRequiredService<ServiceConsumer>();
            var instance = serviceProvider.GetRequiredService<IService>();
            consumer.Service.Dependency.ShouldBe(instance.Dependency);
            consumer.Service.ShouldNotBe(instance);

            // Creates a service provider directly
            var otherServiceProvider = MyComposer.Resolve<IServiceProvider>();
            var otherInstance = otherServiceProvider.GetRequiredService<IService>();
            otherInstance.Dependency.ShouldBe(consumer.Service.Dependency);
        }

        public class ServiceConsumer
        {
            public ServiceConsumer(IService service) =>
                Service = service;

            public IService Service { get; }
        }
        // }
    }
}
