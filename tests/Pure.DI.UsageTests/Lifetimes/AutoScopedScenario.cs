/*
$v=true
$p=5
$d=Auto scoped
$h=You can use the following example to automatically create a session when creating instances of a particular type:
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.AutoScopedScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var program = composition.ProgramRoot;

        // Creates service in session #1
        var service1 = program.CreateService();

        // Creates service in session #2
        var service2 = program.CreateService();

        // Checks that the scoped instances are not identical in different sessions
        service1.Dependency.ShouldNotBe(service2.Dependency);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
partial class Program(Func<IService> serviceFactory)
{
    public IService CreateService() => serviceFactory();
}

partial class Composition
{
    static void Setup() =>
        DI.Setup()
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Scoped).To<Dependency>()
            // Session composition root
            .Root<Service>("SessionRoot", kind: RootKinds.Private)
            // Auto scoped
            .Bind().To(IService (Composition parentScope) =>
            {
                // Creates a new scope from the parent scope
                var scope = new Composition(parentScope);
                // Provides a scope root
                return scope.SessionRoot;
            })

            // Composition root
            .Root<Program>("ProgramRoot");
}
// }