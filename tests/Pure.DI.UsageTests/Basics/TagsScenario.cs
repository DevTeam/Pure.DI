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
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag("Abc")] IDependency dependency1,
    [Tag("Xyz")] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency>("Abc", default).To<AbcDependency>()
            .Bind<IDependency>("Xyz")
            .As(Lifetime.Singleton)
            .To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // "XyzRoot" is root name, "Xyz" is tag
            .Root<IDependency>("XyzRoot", "Xyz")

            // Specifies to create the composition root named "Root"
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency2.ShouldBe(composition.XyzRoot);
        service.Dependency3.ShouldBeOfType<AbcDependency>();
// }            
        composition.SaveClassDiagram();
    }
}