namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class AutowiringWithInitialization
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=03
            // $description=Autowiring with initialization
            // $header=Sometimes instances required some actions before you give them to use - some methods of initialization or fields which should be defined. You can solve these things easily.
            // $footer=:warning: It is not recommended because it is a cause of hidden dependencies.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<INamedService>().To(
                    ctx =>
                    {
                        var service = new InitializingNamedService(ctx.Resolve<IDependency>());
                        // Invokes method "Initialize" for every created instance of this type
                        service.Initialize("Initialized!", ctx.Resolve<IDependency>());
                        return service;
                    });

            // Resolve an instance of interface `IService`
            var instance = AutowiringWithInitializationDI.Resolve<INamedService>();
            
            // Check the instance
            instance.ShouldBeOfType<InitializingNamedService>();

            // Check that the initialization has took place
            instance.Name.ShouldBe("Initialized!");
            // }
        }
    }
}
