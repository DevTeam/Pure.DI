/*
$v=true
$p=6
$d=Tags
$h=Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, _tags_ will help:
$f=The tag can be a constant, a type, or a value of an enumerated type. The _default_ and _null_ tags are also supported.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Basics.TagsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class AbcDependency : IDependency { }
        
class XyzDependency : IDependency { }
        
class Dependency : IDependency { }

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
    
    IDependency Dependency3 { get; }
}

class Service : IService
{
    public Service(
        [Tag("Abc")] IDependency dependency1,
        [Tag("Xyz")] IDependency dependency2,
        IDependency dependency3)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
    
    public IDependency Dependency3 { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>("Abc", default).To<AbcDependency>()
            .Bind<IDependency>("Xyz")
                .As(Lifetime.Singleton)
                .To<XyzDependency>()
                // "XyzRoot" is root name, "Xyz" is tag
                .Root<IDependency>("XyzRoot", "Xyz")
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency2.ShouldBe(composition.XyzRoot);
        service.Dependency3.ShouldBeOfType<AbcDependency>();
// }            
        TestTools.SaveClassDiagram(composition, nameof(TagsScenario));
    }
}