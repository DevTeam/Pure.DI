namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ServiceProvider
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Service provider
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Resolve the instance of IServiceProvider
            var serviceProvider = ServiceProviderDI.Resolve<IServiceProvider>();

            // Get the instance via service provider
            var instance = serviceProvider.GetService(typeof(IService));
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
            serviceProvider.GetService(typeof(INamedService)).ShouldBeNull();
        }
    }
}
