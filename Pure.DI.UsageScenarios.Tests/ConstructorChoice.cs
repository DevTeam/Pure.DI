namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class ConstructorChoice
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=5 Advanced
            // $priority=04
            // $description=Constructor choice
            // $header=We can specify a constructor manually and all its arguments.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To(
                    // Select the constructor and inject required dependencies
                    ctx => new Service(ctx.Resolve<IDependency>(), "some state"));

            var instance = ConstructorChoiceDI.Resolve<IService>();
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
