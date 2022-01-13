namespace Pure.DI.UsageScenarios.Tests
{
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;

    public class MicrosoftDependencyInjection
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=00
            // $description=IServiceProvider/IServiceCollection and Microsoft Dependency Injection
            // $header=In the cases when a project references the Microsoft Dependency Injection library, an extension method for ```IServiceCollection``` is generating automatically with a name like _Add..._ plus the name of a generated class, here it is ```AddMicrosoftDependencyInjectionDI()``` for class ```public class MicrosoftDependencyInjection { }```.
            // {
            DI.Setup()
                // Add Transient
                .Bind<IDependency>().To<Dependency>()
                // Add Scoped
                .Bind<IService>().As(Lifetime.Scoped).To<Service>();

            // Creates a ServiceProvider for the DI specified above.
            var serviceProvider = new ServiceCollection()
                .AddMicrosoftDependencyInjectionDI()
                .BuildServiceProvider();
            
            var instance1 = serviceProvider.GetRequiredService<IService>();
            var instance2 = serviceProvider.GetService(typeof(IService));
            // }
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
        }
    }
}
