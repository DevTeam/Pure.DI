namespace Pure.DI.UsageScenarios.Tests;

public class DependencyTag
{
    [Fact]
    public void Run()
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=04
        // $description=Dependency tag
        // $header=Use a _tag_ to bind several dependencies for the same types.
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // {
        DI.Setup()
            .Bind<IDependency>().Tags("MyDep").To<Dependency>()
            // Configure autowiring and inject dependency tagged by "MyDep"
            .Bind<IService>().To(ctx => new Service(ctx.Resolve<IDependency>("MyDep")));

        // Resolve an instance
        var instance = DependencyTagDI.Resolve<IService>();
        // }
        // Check the instance
        instance.ShouldBeOfType<Service>();
    }
}