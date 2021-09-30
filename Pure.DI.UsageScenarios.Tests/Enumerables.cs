namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class Enumerables
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Enumerables
            // $header=To resolve all possible instances of any tags of the specific type as an _enumerable_ just use the injection _IEnumerable<T>_.
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
                .Bind<CompositionRoot<IEnumerable<IService>>>().To<CompositionRoot<IEnumerable<IService>>>();

            // Resolve all appropriate instances
            var instances = EnumerablesDI.Resolve<CompositionRoot<IEnumerable<IService>>>().Root.ToList();

            // Check the number of resolved instances
            instances.Count.ShouldBe(3);
            // }
            // Check each instance
            instances.ForEach(instance => instance.ShouldBeOfType<Service>());
        }
    }
}
