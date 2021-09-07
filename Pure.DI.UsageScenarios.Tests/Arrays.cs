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
                .Bind<IService>().Tag(1).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>().Tag(2).Tag("abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().Tag(3).To<Service>()
                .Bind<CompositionRoot<IService[]>>()
                    .To<CompositionRoot<IService[]>>();

            // Resolve all appropriate instances
            var composition = ArraysDI.Resolve<CompositionRoot<IService[]>>();
            // }
            // Check the number of resolved instances
            composition.Root.Length.ShouldBe(3);

            foreach (var instance in composition.Root)
            {
                // Check the instance
                instance.ShouldBeOfType<Service>();
            }
        }
    }
}
