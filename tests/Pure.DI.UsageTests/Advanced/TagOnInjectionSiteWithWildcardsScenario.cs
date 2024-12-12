/*
$v=true
$p=6
$d=Tag on injection site with wildcards
$h=The wildcards ‘*’ and ‘?’ are supported.
$f=> [!WARNING]
$f=> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
// {
namespace Pure.DI.UsageTests.Advanced.TagOnInjectionSiteWithWildcardsScenario;

// }
using Pure.DI;
using UsageTests;
using Xunit;

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

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind(Tag.On("*Service:Dependency3", "*Consumer:myDep"))
                .To<AbcDependency>()
            .Bind(Tag.On("*Service:dependency?"))
                .To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // Specifies to create the composition root named "Root"
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<XyzDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency3.ShouldBeOfType<AbcDependency>();
        service.Dependency4.ShouldBeOfType<AbcDependency>();
// }
        composition.SaveClassDiagram();
    }
}