/*
$v=true
$p=1
$d=Generics
$h=Generic types are also supported.
$h=> [!IMPORTANT]
$h=> Instead of open generic types, as in classical DI container libraries, regular generic types with _marker_ types as type parameters are used here. Such "marker" types allow to define dependency graph more precisely.
$h=
$h=For the case of `IDependency<TT>`, `TT` is a _marker_ type, which allows the usual `IDependency<TT>` to be used instead of an open generic type like `IDependency<>`. This makes it easy to bind generic types by specifying _marker_ types such as `TT`, `TT1`, etc. as parameters of generic types:
$f=Actually, the property _Root_ looks like:
$f=```c#
$f=public IService Root
$f={
$f=  get
$f=  {
$f=    return new Service(new Dependency<int>(), new Dependency<string>());
$f=  }
$f=}
$f=```
$f=Even in this simple scenario, it is not possible to precisely define the binding of an abstraction to its implementation using open generic types:
$f=```c#
$f=.Bind(typeof(IMap<,>)).To(typeof(Map<,>))
$f=```
$f=You can try to match them by order or by name derived from the .NET type reflection. But this is not reliable, since order and name matching is not guaranteed. For example, there is some interface with two arguments of type _key and _value_. But in its implementation the sequence of type arguments is mixed up: first comes the _value_ and then the _key_ and the names do not match:
$f=```c#
$f=class Map<TV, TK>: IMap<TKey, TValue> { }
$f=```
$f=At the same time, the marker types `TT1` and `TT2` handle this easily. They determine the exact correspondence between the type arguments in the interface and its implementation:
$f=```c#
$f=.Bind<IMap<TT1, TT2>>().To<Map<TT2, TT1>>()
$f=```
$f=The first argument of the type in the interface, corresponds to the second argument of the type in the implementation and is a _key_. The second argument of the type in the interface, corresponds to the first argument of the type in the implementation and is a _value_. This is a simple example. Obviously, there are plenty of more complex scenarios where tokenized types will be useful.
$f=Marker types are regular .NET types marked with a special attribute, such as:
$f=```c#
$f=[GenericTypeArgument]
$f=internal abstract class TT1 { }
$f=
$f=[GenericTypeArgument]
$f=internal abstract class TT2 { }
$f=```
$f=This way you can easily create your own, including making them fit the constraints on the type parameter, for example:
$f=```c#
$f=[GenericTypeArgument]
$f=internal struct TTS { }
$f=
$f=[GenericTypeArgument]
$f=internal interface TTDisposable: IDisposable { }
$f=
$f=[GenericTypeArgument]
$f=internal interface TTEnumerator<out T>: IEnumerator<T> { }
$f=```
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Generics.GenericsScenario;

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
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind<IDependency<TT>>().To<Dependency<TT>>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.IntDependency.ShouldBeOfType<Dependency<int>>();
        service.StringDependency.ShouldBeOfType<Dependency<string>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService
{
    IDependency<int> IntDependency { get; }

    IDependency<string> StringDependency { get; }
}

class Service(
    IDependency<int> intDependency,
    IDependency<string> stringDependency)
    : IService
{
    public IDependency<int> IntDependency { get; } = intDependency;

    public IDependency<string> StringDependency { get; } = stringDependency;
}
// }