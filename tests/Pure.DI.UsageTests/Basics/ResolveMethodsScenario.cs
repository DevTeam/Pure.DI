/*
$v=true
$p=1
$d=Resolve methods
$h=This example shows how to resolve the roots of a composition using `Resolve` methods to use the composition as a _Service Locator_. The `Resolve` methods are generated automatically without additional effort.
$f=_Resolve_ methods are similar to calls to composition roots. Composition roots are properties (or methods). Their use is efficient and does not cause exceptions. This is why they are recommended to be used. In contrast, _Resolve_ methods have a number of disadvantages:
$f=- They provide access to an unlimited set of dependencies (_Service Locator_).
$f=- Their use can potentially lead to runtime exceptions. For example, when the corresponding root has not been defined.
$f=- Sometimes cannot be used directly, e.g., for MAUI/WPF/Avalonia binding.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.ResolveMethodsScenario;

using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known")]
public class Scenario
{
    [Fact]
    public void Run()
    {
// {    
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<IService>("My Tag").To<OtherService>()

            // Specifies to create a private root
            // that is only accessible from _Resolve_ methods
            .Root<IService>()

            // Specifies to create a public root named _OtherService_
            // using the "My Tag" tag
            .Root<IService>("OtherService", "My Tag");

        var composition = new Composition();

        // The next 3 lines of code do the same thing:
        var service1 = composition.Resolve<IService>();
        var service2 = composition.Resolve(typeof(IService));
        var service3 = composition.Resolve(typeof(IService), null);

        // Resolve by "My Tag" tag
        // The next 3 lines of code do the same thing too:
        var otherService1 = composition.Resolve<IService>("My Tag");
        var otherService2 = composition.Resolve(typeof(IService), "My Tag");
        var otherService3 = composition.OtherService; // Gets the composition through the public root
// }
        service1.ShouldBeOfType<Service>();
        service2.ShouldBeOfType<Service>();
        service3.ShouldBeOfType<Service>();
        otherService1.ShouldBeOfType<OtherService>();
        otherService2.ShouldBeOfType<OtherService>();
        otherService3.ShouldBeOfType<OtherService>();
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class OtherService : IService;
// }