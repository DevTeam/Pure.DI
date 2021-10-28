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
            // $tag=6 Advanced
            // $priority=00
            // $description=Microsoft Dependency Injection
            // $header=In the cases when a project references to Microsoft Dependency Injection and there are no tagged bindings, an IServiceCollection extension method is generated automatically with a name like _Add..._ plus the name of a generated class.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            var serviceProvider = new ServiceCollection()
                .AddMicrosoftDependencyInjectionDI()
                .BuildServiceProvider();
            
            var instance = serviceProvider.GetRequiredService<IService>();
            // }
            instance.ShouldBeOfType<Service>();
        }
    }
}
