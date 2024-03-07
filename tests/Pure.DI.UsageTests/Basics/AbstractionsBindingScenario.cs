/*
$v=true
$p=1
$d=Abstractions binding
$h=You can use the `Bind(...)` method without type parameters. In this case binding will be performed for all abstract types implemented directly and for the implementation type itself. 
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.AbstractionsBindingScenario;

using Xunit;

// {
interface IDependency;

interface IOtherDependency;

class Dependency: IDependency, IOtherDependency;

class Service(
    Dependency dependencyImpl,
    IDependency dependency,
    IOtherDependency otherDependency);
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {        
        // Specifies to create a partial class "Composition"
        DI.Setup("Composition")
            // Begins the definition of the binding for all abstract types
            // that are directly implemented and the implementation type itself.
            .Bind().As(Lifetime.PerBlock).To<Dependency>()
            // Specifies to create a property "MyService"
            .Root<Service>("MyService");
        
        var composition = new Composition();
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}