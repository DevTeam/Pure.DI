/*
$v=true
$p=0
$d=Generics
$sa=Generic composition roots
$sa=Complex generics
$sa=Custom generic argument
$h=Generic types are supported out of the box: a single binding like `Bind<IRepository<TT>>().To<Repository<TT>>()` covers `IRepository<User>`, `IRepository<Order>` and any other instantiation used in the object graph. Since Pure.DI is a source generator, each of them is turned into concrete, reflection-free code at compile time.
$h=>[!IMPORTANT]
$h=>Instead of open generic types, as in classical DI container libraries, regular generic types with `marker` types as type parameters are used here. Such "marker" types allow to define dependency graph more precisely.
$h=
$h=For the case of `IRepository<TT>`, `TT` is a `marker` type, which allows the usual `IRepository<TT>` to be used instead of an open generic type like `IRepository<>`. This makes it easy to bind generic types by specifying `marker` types such as `TT`, `TT1`, etc. as parameters of generic types:
$f=Actually, the property `DataService` looks like:
$f=```c#
$f=public IDataService DataService
$f={
$f=  get
$f=  {
$f=    return new DataService(new Repository<User>(), new Repository<Order>());
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
// ReSharper disable ClassNeverInstantiated.Global
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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Disable Resolve methods to keep the public API minimal
            .Hint(Hint.Resolve, "Off")
            // Binding a generic interface to a generic implementation
            // using the marker type TT. This allows Pure.DI to match
            // IRepository<User> to Repository<User>, IRepository<Order> to Repository<Order>, etc.
            .Bind<IRepository<TT>>().To<Repository<TT>>()
            .Bind<IDataService>().To<DataService>()

            // Composition root
            .Root<IDataService>("DataService");

        var composition = new Composition();
        var dataService = composition.DataService;

        // Verifying that the correct generic types were injected
        dataService.Users.ShouldBeOfType<Repository<User>>();
        dataService.Orders.ShouldBeOfType<Repository<Order>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IRepository<T>;

class Repository<T> : IRepository<T>;

// Domain entities
record User;

record Order;

interface IDataService
{
    IRepository<User> Users { get; }

    IRepository<Order> Orders { get; }
}

class DataService(
    IRepository<User> users,
    IRepository<Order> orders)
    : IDataService
{
    public IRepository<User> Users { get; } = users;

    public IRepository<Order> Orders { get; } = orders;
}
// }