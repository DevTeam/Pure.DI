/*
$v=true
$p=1
$d=Simplified binding
$h=You can use the `Bind(...)` method without type parameters. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.
$f=As practice has shown, in most cases it is possible to define abstraction types in bindings automatically. That's why we added API `Bind()` method without type parameters to define abstractions in bindings. It is the `Bind()` method that performs the binding:
$f=
$f=- with the implementation type itself
$f=- and if it is NOT an abstract type or structure
$f=  - with all abstract types that it directly implements
$f=  - exceptions are special types
$f=
$f=Special types will not be added to bindings:
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
$f=
$f=For class `Dependency`, the `Bind().To<Dependency>()` binding will be equivalent to the `Bind<IDependency, IOtherDependency, Dependency>().To<Dependency>()` binding. The types `IDisposable`, `IEnumerable<string>` did not get into the binding because they are special from the list above. `DependencyBase` did not get into the binding because it is not abstract. `IDependencyBase` is not included because it is not implemented directly by class `Dependency`.
$f=
$f=|   |                       |                                                 |
$f=|---|-----------------------|-------------------------------------------------|
$f=| ✅ | `Dependency`          | implementation type itself                      |
$f=| ✅ | `IDependency`         | directly implements                             |
$f=| ✅ | `IOtherDependency`    | directly implements                             |
$f=| ❌ | `IDisposable`         | special type                                    |
$f=| ❌ | `IEnumerable<string>` | special type                                    |
$f=| ❌ | `DependencyBase`      | non-abstract                                    |
$f=| ❌ | `IDependencyBase`     | is not directly implemented by class Dependency |
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.SimplifiedBindingScenario;

using System.Collections;
using Xunit;

// {
interface IDependencyBase;

class DependencyBase: IDependencyBase;

interface IDependency;

interface IOtherDependency;

class Dependency:
    DependencyBase,
    IDependency,
    IOtherDependency,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() => throw new NotImplementedException();

    public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IService;

class Service(
    Dependency dependencyImpl,
    IDependency dependency,
    IOtherDependency otherDependency)
    : IService;
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
            .Bind().To<Service>()
            
            // Specifies to create a property "MyService"
            .Root<IService>("MyService");
        
        var composition = new Composition();
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}