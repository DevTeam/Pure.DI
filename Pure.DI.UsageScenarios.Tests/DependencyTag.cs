namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class DependencyTag
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=04
            // $description=Dependency tag
            // $header=Use a _tag_ to bind few dependencies for the same types.
            // {
            DI.Setup()
                .Bind<IDependency>().Tag("MyDep").To<Dependency>()
                // Configure autowiring and inject dependency tagged by "MyDep"
                .Bind<IService>().To(ctx => new Service(ctx.Resolve<IDependency>("MyDep")));

            // Resolve an instance
            var instance = DependencyTagDI.Resolve<IService>();
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
