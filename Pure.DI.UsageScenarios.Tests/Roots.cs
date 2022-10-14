// ReSharper disable ArrangeNamespaceBody
// ReSharper disable PossibleMultipleEnumeration
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
            // $tag=1 Basics
            // $priority=02
            // $description=Roots
            // $header=Specifying of root explicitly is needed when we would like to use it as a composition root for the following cases:
            // $header=- the type could not be fully defined in the compile time, for instance, it was not used as a some dependency
            // $header=- when we would like to use some enumeration, array or other collection as a composition root
            // $header=- it is generic type and is is opened (so it uses generic parameter markers)
            // $header=To specify a composition root explicitly use the `Root<T>()` call as in the sample above: 
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
                .Root<IService<int>>();
            
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
