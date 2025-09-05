/*
$v=true
$p=7
$d=Inheritance of compositions
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.InheritanceOfCompositionsScenario;

using Pure.DI;
using Shouldly;
using Xunit;
using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        var composition = new Composition();
        var service = composition.Root;
// }
        service.ShouldBeOfType<Program>();
    }
}

// {
class BaseComposition
{
    private static void Setup() =>
        DI.Setup(kind: Internal)
            .Bind<IDependency>().To<Dependency>();
}

partial class Composition: BaseComposition
{
    private void Setup() =>
        DI.Setup()
            .Bind<IService>().To<Service>()
            .Root<Program>(nameof(Root));
}

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Program(IService service)
{
    public IService Service { get; } = service;
}
// }