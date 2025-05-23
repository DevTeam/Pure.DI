﻿/*
$v=true
$p=6
$d=Tags
$h=Sometimes it's important to take control of building a dependency graph. For example, when there are different implementations of the same interface. In this case, _tags_ will help:
$f=The example shows how to:
$f=- Define multiple bindings for the same interface
$f=- Use tags to differentiate between implementations
$f=- Control lifetime management
$f=- Inject tagged dependencies into constructors
$f=
$f=The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. The _default_ and _null_ tags are also supported.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
namespace Pure.DI.UsageTests.Basics.TagsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // The `default` tag is used to resolve dependencies
            // when the tag was not specified by the consumer
            .Bind<IDependency>("AbcTag", default).To<AbcDependency>()
            .Bind<IDependency>("XyzTag").As(Lifetime.Singleton).To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // "XyzRoot" is root name, "XyzTag" is tag
            .Root<IDependency>("XyzRoot", "XyzTag")

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
    [Tag("AbcTag")] IDependency dependency1,
    [Tag("XyzTag")] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
// }