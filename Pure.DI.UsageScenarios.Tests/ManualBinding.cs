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
            // $header=We can specify a constructor manually with all its arguments and even call some initializing methods.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To(
                    // Select the constructor and inject required dependencies
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
