// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class ArrayBindingOverride
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=10
            // $description=Array binding override
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind to the implementation #1
                .Bind<IService>(1).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>(99).Tags(2, "abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().Tags(3).To<Service>()
                // Bind array
                .Bind<IService[]>().To(ctx => new[] {ctx.Resolve<IService>(1), ctx.Resolve<IService>("abc")});

            var array = ArrayBindingOverrideDI.Resolve<IService[]>();
            // }
            // Check the number of resolved instances
            array.Length.ShouldBe(2);

            foreach (var instance in array)
            {
                // Check the instance
                instance.ShouldBeOfType<Service>();
            }
        }
    }
}
