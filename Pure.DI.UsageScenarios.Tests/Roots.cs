// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class Roots
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Roots
            // $header=To specify composition roots explicitly use the `Root<T>()` call.
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService<TT>>().To<Service<TT>>()
                // Bind to the implementation #1
                .Bind<IService>().Tags(1).To<Service>()
                // Bind to the implementation #2
                .Bind<IService>().Tags(2, "abc").To<Service>()
                // Explicitly specifies to provide the composition root of type IEnumerable<IService>
                .Root<IEnumerable<IService>>()
                // Explicitly specifies to provide the composition root of type IService<int>
                .Root<IEnumerable<IService<int>>>();
            
            var instances = RootsDI.Resolve<IEnumerable<IService>>();
            var instances2 = RootsDI.ResolveIEnumerableIService();
            var service = RootsDI.Resolve<IService<int>>();
            var service2 = RootsDI.ResolveServiceInt();

            // Check the number of resolved instances
            instances.Count().ShouldBe(2);
            instances2.Count().ShouldBe(2);
            // }
            instances.ShouldNotBe(instances2);
            service.ShouldNotBe(service2);
        }
    }
}
