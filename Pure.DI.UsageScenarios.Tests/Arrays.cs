// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class Arrays
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Arrays
            // $header=To resolve all possible instances of any tags of the specific type as an _array_ just use the injection of _T[]_.
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind to the implementation #1
                .Bind<IService>(1).As(Lifetime.PerResolve).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>(99).Tags(2, "abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().Tags(3).To<Service>()
                // Explicitly specifies to provide the composition root of type IService[]
                .Root<IService[]>();

            // Resolve all appropriate instances
            var array = ArraysDI.Resolve<IService[]>();
            // }
            // Check the number of resolved instances
            array.Length.ShouldBe(3);

            foreach (var instance in array)
            {
                // Check the instance
                instance.ShouldBeOfType<Service>();
            }
        }
    }
}
