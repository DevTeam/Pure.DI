namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;
    using static Lifetime;

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
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind to the implementation #1
                .Bind<IService>(1).As(PerResolve).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>(2).Tags("abc").To<Service>()
                // Bind to the implementation #3
                .Bind<IService>().As(Singleton).Tags(3).To<Service>()
                .Bind<CompositionRoot<ICollection<IService>>>().To<CompositionRoot<ICollection<IService>>>();

            // Resolve all appropriate instances
            var composition = CollectionsDI.ResolveCompositionRootICollectionIService();

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
