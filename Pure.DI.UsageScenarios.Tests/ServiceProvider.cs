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
            // $tag=1 Basics
            // $priority=01
            // $description=Service provider
            // $header=It is easy to get an instance of the _IServiceProvider_ type at any time without any additional effort.
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
