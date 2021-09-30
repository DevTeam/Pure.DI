namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class Sets
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Sets
            // $header=To resolve all possible instances of any tags of the specific type as a _ISet<>_ just use the injection _ISet<T>_.
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind to the implementation #1
                .Bind<IService>().Tags(1).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>().Tags(2, "abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().Tags(3).To<Service>()
                .Bind<CompositionRoot<ISet<IService>>>().To<CompositionRoot<ISet<IService>>>();

            // Resolve all appropriate instances
            var instances = SetsDI.Resolve<CompositionRoot<ISet<IService>>>().Root;

            // Check the number of resolved instances
            instances.Count.ShouldBe(3);
            // }
            foreach (var instance in instances)
            {
                // Check each instance
                instance.ShouldBeOfType<Service>();
            }
        }
    }
}
