/*
$v=true
$p=2
$d=Factory
$h=This example demonstrates how to create and initialize an instance manually. At the compilation stage, the set of dependencies that the object to be created needs is determined. In most cases, this happens automatically, according to the set of constructors and their arguments, and does not require additional customization efforts. But sometimes it is necessary to manually create and/or initialize an object, as in lines of code:
$f=There are scenarios where manual control over the creation process is required, such as
$f=- When additional initialization logic is needed
$f=- When complex construction steps are required
$f=- When specific object states need to be set during creation
$f=
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Basics.FactoryScenario;

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
            .Bind<IDependency>().To<IDependency>(ctx =>
            {
                // Some logic for creating an instance:
                ctx.Inject(out Dependency dependency);
                dependency.Initialize();
                return dependency;
            })
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        service.Dependency.IsInitialized.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    bool IsInitialized { get; }
}

class Dependency : IDependency
{
    public bool IsInitialized { get; private set; }

    public void Initialize() => IsInitialized = true;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
// }