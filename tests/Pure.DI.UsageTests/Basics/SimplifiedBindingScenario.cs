/*
$v=true
$p=1
$d=Simplified binding
$h=You can use the `Bind(...)` method without type parameters. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.
$f=Special types from the list above will not be added to bindings:
$f=
$f=- `System.Object`
$f=- `System.Enum`
$f=- `System.MulticastDelegate`
$f=- `System.Delegate`
$f=- `System.Collections.IEnumerable`
$f=- `System.Collections.Generic.IEnumerable<T>`
$f=- `System.Collections.Generic.IList<T>`
$f=- `System.Collections.Generic.ICollection<T>`
$f=- `System.Collections.IEnumerator`
$f=- `System.Collections.Generic.IEnumerator<T>`
$f=- `System.Collections.Generic.IIReadOnlyList<T>`
$f=- `System.Collections.Generic.IReadOnlyCollection<T>`
$f=- `System.IDisposable`
$f=- `System.IAsyncResult`
$f=- `System.AsyncCallback` 
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.SimplifiedBindingScenario;

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
            // and if the implementation is not an abstract class or structure,
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