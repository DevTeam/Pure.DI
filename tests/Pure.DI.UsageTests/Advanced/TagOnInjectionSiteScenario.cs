﻿/*
$v=true
$p=5
$d=Tag on injection site
$h=Sometimes it is necessary to determine which binding will be used to inject explicitly. To do this, use a special tag created by calling the `Tag.On()` method. Tag on injection site is specified in a special format: `Tag.On("<namespace>.<type>.<member>[:argument]")`. The argument is specified only for the constructor and methods. For example, for namespace _MyNamespace_ and type _Class1_:
$h=
$h=- `Tag.On("MyNamespace.Class1.Class1:state1") - the tag corresponds to the constructor argument named _state_ of type _MyNamespace.Class1_
$h=- `Tag.On("MyNamespace.Class1.DoSomething:myArg")` - the tag corresponds to the _myArg_ argument of the _DoSomething_ method
$h=- `Tag.On("MyNamespace.Class1.MyData")` - the tag corresponds to property or field _MyData_
$h=
$h=The wildcards `*` and `?` are supported. All names are case-sensitive. The global namespace prefix `global::` must be omitted. You can also combine multiple tags in a single `Tag.On("...", "...")` call.
$h=
$h=For generic types, the type name also contains the number of type parameters, e.g., for the `myDep` constructor argument of the `Consumer<T>` class, the tag on the injection site would be ``MyNamespace.Consumer`1.Consumer:myDep``:
$f=> [!WARNING]
$f=> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.

namespace Pure.DI.UsageTests.Advanced;

using Pure.DI;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(
                Tag.On("*Service.Service:dependency1"),
                // Tag on injection site for generic type
                Tag.On("*Consumer`1.Consumer:myDep"))
                .To<AbcDependency>()
            .Bind(
                // Combined tag
                Tag.On(
                    "*Service.Service:dependency2",
                    "*Service:Dependency3"))
                .To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // Specifies to create the composition root named "Root"
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency3.ShouldBeOfType<XyzDependency>();
        service.Dependency4.ShouldBeOfType<AbcDependency>();
// }
        composition.SaveClassDiagram("TagOnInjectionSiteScenario");
    }
}

// {
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }

    IDependency Dependency4 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public required IDependency Dependency3 { init; get; }

    public IDependency Dependency4 => consumer.Dependency;
}
// }