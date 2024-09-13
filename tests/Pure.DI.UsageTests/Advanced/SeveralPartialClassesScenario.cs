/*
$v=true
$p=11
$d=A few partial classes
$h=The setting code for one Composition can be located in several methods and/or in several partial classes.
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.SeveralPartialClassesScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Composition
{
    // This method will not be called in runtime
    static void Setup1() =>
        DI.Setup()
            .Bind<IDependency>().To<Dependency>();
}

partial class Composition
{
    // This method will not be called in runtime
    static void Setup2() =>
        DI.Setup()
            .Bind<IService>().To<Service>();
}

partial class Composition
{
    // This method will not be called in runtime
    private static void Setup3() =>
        DI.Setup()
            .Root<IService>("Root");
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();
        var service = composition.Root;
// }            
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}