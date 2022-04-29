// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class ManualBinding
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=01
            // $description=Manual binding
            // $header=We can specify a constructor manually with all its arguments and even call some methods before an instance will be returned to consumers. Would also like to point out that invocations like *__ctx.Resolve<>()__* will be replaced by a related expression to create a required composition for the performance boost where possible, except when it might cause a circular dependency.
            // $footer=The actual composition for the example above looks like this:
            // $footer=```CSharp
            // $footer=new Service(new Dependency()), "some state");
            // $footer=```
            // $footer=... and no any additional method calls. This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To(
                    // Select the constructor and inject required dependencies manually
                    ctx => new Service(ctx.Resolve<IDependency>(), "some state"));

            var instance = ManualBindingDI.Resolve<IService>();
            // }
            // Check the type
            instance.ShouldBeOfType<Service>();
            // {

            // Check the injected constant
            instance.State.ShouldBe("some state");
            // }
        }
    }
}
