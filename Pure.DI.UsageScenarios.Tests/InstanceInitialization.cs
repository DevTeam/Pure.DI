namespace Pure.DI.UsageScenarios.Tests;

public class InstanceInitialization
{
    [Fact]
    public void Run()
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=03
        // $description=Instance initialization
        // $header=Sometimes instances required some actions before you give them to use - some methods of initialization or fields which should be defined. You can solve these things easily. :warning: But this approach is not recommended because it is a cause of hidden dependencies. 
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
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
        var instance = InstanceInitializationDI.Resolve<INamedService>();

        // Check the instance
        instance.ShouldBeOfType<InitializingNamedService>();

        // Check that the initialization has took place
        instance.Name.ShouldBe("Initialized!");
        // }
    }
}