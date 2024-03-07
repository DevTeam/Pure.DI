/*
$v=true
$p=1
$d=Abstractions binding
$h=You can use the `Bind(...)` method without type parameters. In this case binding will be performed for the implementation type itself, and if the implementation is a class or structure, for all abstract but NOT special types that are directly implemented.
$h=Special types include:
$h=
$h=- `System.Object`
$h=- `System.Enum`
$h=- `System.MulticastDelegate`
$h=- `System.Delegate`
$h=- `System.Collections.IEnumerable`
$h=- `System.Collections.Generic.IEnumerable&lt;T&gt;`
$h=- `System.Collections.Generic.IList&lt;T&gt;`
$h=- `System.Collections.Generic.ICollection&lt;T&gt;`
$h=- `System.Collections.IEnumerator`
$h=- `System.Collections.Generic.IEnumerator&lt;T&gt;`
$h=- `System.Collections.Generic.IIReadOnlyList&lt;T&gt;`
$h=- `System.Collections.Generic.IReadOnlyCollection&lt;T&gt;`
$h=- `System.IDisposable`
$h=- `System.IAsyncResult`
$h=- `System.AsyncCallback` 
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
            // Begins the binding definition for the implementation type itself,
            // and if the implementation is a class or structure,
            // for all abstract but NOT special types that are directly implemented.
            // So that's the equivalent of the following:
            // .Bind<IDependency, IOtherDependency, Dependency>()
            //  .As(Lifetime.PerBlock)
            //  .To<Dependency>()
            .Bind().As(Lifetime.PerBlock).To<Dependency>()
            // Specifies to create a property "MyService"
            .Root<Service>("MyService");
        
        var composition = new Composition();
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}