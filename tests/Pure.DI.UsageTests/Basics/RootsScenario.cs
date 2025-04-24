/*
$v=true
$p=20
$d=Roots
$h=Sometimes you need roots for all types inherited from <see cref="T"/> available at compile time at the point where the method is called.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.RootsScenario;

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
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .Roots<IService>("My{type}");

        var composition = new Composition();
        composition.MyService1.ShouldBeOfType<Service1>();
        composition.MyService2.ShouldBeOfType<Service2>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service1(IDependency dependency) : IService;

class Service2(IDependency dependency) : IService;
// }