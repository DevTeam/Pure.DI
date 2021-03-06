namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class Collections
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Collections
            // $header=To resolve all possible instances of any tags of the specific type as a _collection_ just use the injection _ICollection<T>_
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind to the implementation #1
                .Bind<IService>().Tag(1).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>().Tag(2).Tag("abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().Tag(3).To<Service>()
                .Bind<CompositionRoot<ICollection<IService>>>().To<CompositionRoot<ICollection<IService>>>();

            // Resolve all appropriate instances
            var composition = CollectionsDI.Resolve<CompositionRoot<ICollection<IService>>>();

            // Check the number of resolved instances
            composition.Root.Count.ShouldBe(3);
            // }
            foreach (var instance in composition.Root)
            {
                // Check each instance
                instance.ShouldBeOfType<Service>();
            }
        }
    }
}
