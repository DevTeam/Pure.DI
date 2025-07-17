/*
$v=true
$p=3
$d=Tag Any
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
namespace Pure.DI.UsageTests.Advanced.TagAnyScenario;

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
            .Bind<IDependency>(Tag.Any).To(ctx => new Dependency(ctx.Tag))
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root")

            // Root by Tag.Any
            .Root<IDependency>("OtherDependency", "Other");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.Key.ShouldBe("Abc");
        service.Dependency2.Key.ShouldBe(123);
        service.Dependency3.Key.ShouldBeNull();
        composition.OtherDependency.Key.ShouldBe("Other");
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    object? Key { get; }
}

record Dependency(object? Key) : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag("Abc")] IDependency dependencyAbc,
    [Tag(123)] Func<IDependency> dependency123Factory,
    IDependency dependency)
    : IService
{
    public IDependency Dependency1 { get; } = dependencyAbc;

    public IDependency Dependency2 { get; } = dependency123Factory();

    public IDependency Dependency3 { get; } = dependency;
}
// }