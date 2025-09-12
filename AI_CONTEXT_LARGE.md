This Markdown-formatted document contains information about working with Pure.DI

# Usage scenarios.

## Auto-bindings

Injection of non-abstract types is possible without any additional effort.

```c#
using Pure.DI;

// Specifies to create a partial class with name "Composition"
DI.Setup("Composition")
    // with the root "MyService"
    .Root<Service>("MyService");

var composition = new Composition();

// service = new Service(new Dependency())
var service = composition.MyService;

class Dependency;

class Service(Dependency dependency);
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!WARNING]
> But this approach cannot be recommended if you follow the dependency inversion principle and want your types to depend only on abstractions. Or you want to precisely control the lifetime of a dependency.

It is better to inject abstract dependencies, for example, in the form of interfaces. Use bindings to map abstract types to their implementations as in almost all [other examples](injections-of-abstractions.md).

## Injections of abstractions

This example demonstrates the recommended approach of using abstractions instead of implementations when injecting dependencies.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Binding abstractions to their implementations
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create a composition root
    // of type "Program" with the name "Root"
    .Root<Program>("Root");

var composition = new Composition();

// var root = new Program(new Service(new Dependency()));
var root = composition.Root;

root.Run();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    void DoSomething();
}

class Service(IDependency dependency) : IService
{
    public void DoSomething()
    {
    }
}

partial class Program(IService service)
{
    public void Run() => service.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Usually the biggest block in the setup is the chain of bindings, which describes which implementation corresponds to which abstraction. This is necessary so that the code generator can build a composition of objects using only NOT abstract types. This is true because the cornerstone of DI technology implementation is the principle of abstraction-based programming rather than concrete class-based programming. Thanks to it, it is possible to replace one concrete implementation by another. And each implementation can correspond to an arbitrary number of abstractions.
> [!TIP]
> Even if the binding is not defined, there is no problem with the injection, but obviously under the condition that the consumer requests an injection NOT of abstract type.


## Composition roots

This example demonstrates several ways to create a composition root.
> [!TIP]
> There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.

If you use classic DI containers, the composition is resolved dynamically every time you call a method similar to `T Resolve<T>()` or `object GetService(Type type)`. The root of the composition there is simply the root type of the composition of objects in memory `T` or `Type` type. There can be as many of these as you like. In the case of Pure.DI, the number of composition roots is limited because for each composition root a separate property or method is created at compile time. Therefore, each root is defined explicitly by calling the `Root(string rootName)` method.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IService>().To<Service>()
    .Bind<IService>("Other").To<OtherService>()
    .Bind<IDependency>().To<Dependency>()

    // Specifies to create a regular composition root
    // of type "IService" with the name "MyService"
    .Root<IService>("MyService")

    // Specifies to create an anonymous composition root
    // that is only accessible from "Resolve()" methods
    .Root<IDependency>()

    // Specifies to create a regular composition root
    // of type "IService" with the name "MyOtherService"
    // using the "Other" tag
    .Root<IService>("MyOtherService", "Other");

var composition = new Composition();

// service = new Service(new Dependency());
var service = composition.MyService;

// someOtherService = new OtherService();
var someOtherService = composition.MyOtherService;

// All and only the roots of the composition
// can be obtained by Resolve method
var dependency = composition.Resolve<IDependency>();
        
// including tagged ones
var tagged = composition.Resolve<IService>("Other");

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class OtherService : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

The name of the composition root is arbitrarily chosen depending on its purpose, but should be restricted by the property naming conventions in C# since it is the same name as a property in the composition class. In reality, the _Root_ property has the form:
```c#
public IService Root
{
  get
  {
    return new Service(new Dependency());
  }
}
```
To avoid generating _Resolve_ methods just add a comment `// Resolve = Off` before a _Setup_ method:
```c#
// Resolve = Off
DI.Setup("Composition")
  .Bind<IDependency>().To<Dependency>()
  ...
```
This can be done if these methods are not needed, in case only certain composition roots are used. It's not significant then, but it will help save resources during compilation.

## Resolve methods

This example shows how to resolve the roots of a composition using `Resolve` methods to use the composition as a _Service Locator_. The `Resolve` methods are generated automatically without additional effort.

```c#
using Pure.DI;

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

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class OtherService : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

_Resolve_ methods are similar to calls to composition roots. Composition roots are properties (or methods). Their use is efficient and does not cause exceptions. This is why they are recommended to be used. In contrast, _Resolve_ methods have a number of disadvantages:
- They provide access to an unlimited set of dependencies (_Service Locator_).
- Their use can potentially lead to runtime exceptions. For example, when the corresponding root has not been defined.
- Sometimes cannot be used directly, e.g., for MAUI/WPF/Avalonia binding.

## Simplified binding

You can use the `Bind(...)` method without type parameters. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.

```c#
using System.Collections;
using Pure.DI;

// Specifies to create a partial class "Composition"
DI.Setup(nameof(Composition))
    // Begins the binding definition for the implementation type itself,
    // and if the implementation is not an abstract class or structure,
    // for all abstract but NOT special types that are directly implemented.
    // So that's the equivalent of the following:
    // .Bind<IDependency, IOtherDependency, Dependency>()
    //   .As(Lifetime.PerBlock)
    //   .To<Dependency>()
    .Bind().As(Lifetime.PerBlock).To<Dependency>()
    .Bind().To<Service>()

    // Specifies to create a property "MyService"
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;

interface IDependencyBase;

class DependencyBase : IDependencyBase;

interface IDependency;

interface IOtherDependency;

class Dependency :
    DependencyBase,
    IDependency,
    IOtherDependency,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() { }

    public IEnumerator<string> GetEnumerator() =>
        new List<string> { "abc" }.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IService;

class Service(
    Dependency dependencyImpl,
    IDependency dependency,
    IOtherDependency otherDependency)
    : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

As practice has shown, in most cases it is possible to define abstraction types in bindings automatically. That's why we added API `Bind()` method without type parameters to define abstractions in bindings. It is the `Bind()` method that performs the binding:

- with the implementation type itself
- and if it is NOT an abstract type or structure
  - with all abstract types that it directly implements
  - exceptions are special types

Special types will not be added to bindings:

- `System.Object`
- `System.Enum`
- `System.MulticastDelegate`
- `System.Delegate`
- `System.Collections.IEnumerable`
- `System.Collections.Generic.IEnumerable<T>`
- `System.Collections.Generic.IList<T>`
- `System.Collections.Generic.ICollection<T>`
- `System.Collections.IEnumerator`
- `System.Collections.Generic.IEnumerator<T>`
- `System.Collections.Generic.IIReadOnlyList<T>`
- `System.Collections.Generic.IReadOnlyCollection<T>`
- `System.IDisposable`
- `System.IAsyncResult`
- `System.AsyncCallback`

For class `Dependency`, the `Bind().To<Dependency>()` binding will be equivalent to the `Bind<IDependency, IOtherDependency, Dependency>().To<Dependency>()` binding. The types `IDisposable`, `IEnumerable<string>` did not get into the binding because they are special from the list above. `DependencyBase` did not get into the binding because it is not abstract. `IDependencyBase` is not included because it is not implemented directly by class `Dependency`.

|   |                       |                                                 |
|---|-----------------------|-------------------------------------------------|
| ✅ | `Dependency`          | implementation type itself                      |
| ✅ | `IDependency`         | directly implements                             |
| ✅ | `IOtherDependency`    | directly implements                             |
| ❌ | `IDisposable`         | special type                                    |
| ❌ | `IEnumerable<string>` | special type                                    |
| ❌ | `DependencyBase`      | non-abstract                                    |
| ❌ | `IDependencyBase`     | is not directly implemented by class Dependency |

## Factory

This example demonstrates how to create and initialize an instance manually. At the compilation stage, the set of dependencies that the object to be created needs is determined. In most cases, this happens automatically, according to the set of constructors and their arguments, and does not require additional customization efforts. But sometimes it is necessary to manually create and/or initialize an object, as in lines of code:

```c#
using Shouldly;
using Pure.DI;

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
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

There are scenarios where manual control over the creation process is required, such as:
- When additional initialization logic is needed
- When complex construction steps are required
- When specific object states need to be set during creation

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Simplified factory

This example shows how to create and initialize an instance manually in a simplified form. When you use a lambda function to specify custom instance initialization logic, each parameter of that function represents an injection of a dependency. Starting with C# 10, you can also put the `Tag(...)` attribute in front of the parameter to specify the tag of the injected dependency.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("now").To(_ => DateTimeOffset.Now)
    // Injects Dependency and DateTimeOffset instances
    // and performs further initialization logic
    // defined in the lambda function
    .Bind<IDependency>().To((
        Dependency dependency,
        [Tag("now")] DateTimeOffset time) =>
    {
        dependency.Initialize(time);
        return dependency;
    })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Dependency.IsInitialized.ShouldBeTrue();

interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

class Dependency : IDependency
{
    public DateTimeOffset Time { get; private set; }

    public bool IsInitialized { get; private set; }

    public void Initialize(DateTimeOffset time)
    {
        Time = time;
        IsInitialized = true;
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example creates a `service` that depends on a `dependency` initialized with a specific timestamp. The `Tag` attribute allows specifying named dependencies for more complex scenarios.

## Injection on demand

This example demonstrates using dependency injection with Pure.DI to dynamically create dependencies as needed via a factory function. The code defines a service (`Service`) that requires multiple instances of a dependency (`Dependency`). Instead of injecting pre-created instances, the service receives a `Func<IDependency>` factory delegate, allowing it to generate dependencies on demand.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Count.ShouldBe(2);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IReadOnlyList<IDependency> Dependencies { get; }
}

class Service(Func<IDependency> dependencyFactory): IService
{
    public IReadOnlyList<IDependency> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Key elements:
- `Dependency` is bound to the `IDependency` interface, and `Service` is bound to `IService`.
- The `Service` constructor accepts `Func<IDependency>`, enabling deferred creation of dependencies.
- The `Service` calls the factory twice, resulting in two distinct `Dependency` instances stored in its `Dependencies` collection.

This approach showcases how factories can control dependency lifetime and instance creation timing in a DI container. The Pure.DI configuration ensures the factory resolves new `IDependency` instances each time it's invoked, achieving "injections as required" functionality.

## Injections on demand with arguments

This example illustrates dependency injection with parameterized factory functions using Pure.DI, where dependencies are created with runtime-provided arguments. The scenario features a service that generates dependencies with specific IDs passed during instantiation.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = service.Dependencies;
dependencies.Count.ShouldBe(2);
dependencies[0].Id.ShouldBe(33);
dependencies[1].Id.ShouldBe(99);

interface IDependency
{
    int Id { get; }
}

class Dependency(int id) : IDependency
{
    public int Id { get; } = id;
}

interface IService
{
    IReadOnlyList<IDependency> Dependencies { get; }
}

class Service(Func<int, IDependency> dependencyFactoryWithArgs): IService
{
    public IReadOnlyList<IDependency> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Key components:
- `Dependency` class accepts an int id constructor argument, stored in its `Id` property.
- `Service` receives `Func<int, IDependency>` delegate, enabling creation of dependencies with dynamic values.
- `Service` creates two dependencies using the factory – one with ID `33`, another with ID `99`.

Delayed dependency instantiation:
- Injection of dependencies requiring runtime parameters
- Creation of distinct instances with different configurations
- Type-safe resolution of dependencies with constructor arguments

## Class arguments

Sometimes you need to pass some state to a composition class to use it when resolving dependencies. To do this, just use the `Arg<T>(string argName)` method, specify the type of argument and its name. You can also specify a tag for each argument. You can then use them as dependencies when building the object graph. If you have multiple arguments of the same type, just use tags to distinguish them. The values of the arguments are manipulated when you create a composition class by calling its constructor. It is important to remember that only those arguments that are used in the object graph will appear in the constructor. Arguments that are not involved will not be added to the constructor arguments.
> [!NOTE]
> Actually, class arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root "MyRoot"
    .Root<IService>("MyService")

    // Some kind of identifier
    .Arg<int>("id")

    // An argument can be tagged (e.g., tag "my service name")
    // to be injectable by type and this tag
    .Arg<string>("serviceName", "my service name")
    .Arg<string>("dependencyName");

var composition = new Composition(id: 123, serviceName: "Abc", dependencyName: "Xyz");

// service = new Service("Abc", new Dependency(123, "Xyz"));
var service = composition.MyService;

service.Name.ShouldBe("Abc");
service.Dependency.Id.ShouldBe(123);
service.Dependency.Name.ShouldBe("Xyz");

interface IDependency
{
    int Id { get; }

    string Name { get; }
}

class Dependency(int id, string name) : IDependency
{
    public int Id { get; } = id;

    public string Name { get; } = name;
}

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service(
    // The tag allows to specify the injection point accurately.
    // This is useful, for example, when the type is the same.
    [Tag("my service name")] string name,
    IDependency dependency) : IService
{
    public string Name { get; } = name;

    public IDependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Root arguments

Sometimes it is necessary to pass some state to the composition to use it when resolving dependencies. To do this, just use the `RootArg<T>(string argName)` method, specify the type of argument and its name. You can also specify a tag for each argument. You can then use them as dependencies when building the object graph. If you have multiple arguments of the same type, just use tags to distinguish them. The root of a composition that uses at least one root argument is prepended as a method, not a property. It is important to remember that the method will only display arguments that are used in the object graph of that composition root. Arguments that are not involved will not be added to the method arguments. It is best to use unique argument names so that there are no collisions.
> [!NOTE]
> Actually, root arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Tag;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Some argument
    .RootArg<int>("id")
    .RootArg<string>("dependencyName")

    // An argument can be tagged (e.g., tag "forService")
    // to be injectable by type and this tag
    .RootArg<string>("serviceName", ForService)

    // Composition root
    .Root<IService>("CreateServiceWithArgs");

var composition = new Composition();

// service = new Service("Abc", new Dependency(123, "dependency 123"));
var service = composition.CreateServiceWithArgs(serviceName: "Abc", id: 123, dependencyName: "dependency 123");

service.Name.ShouldBe("Abc");
service.Dependency.Id.ShouldBe(123);
service.Dependency.DependencyName.ShouldBe("dependency 123");

interface IDependency
{
    int Id { get; }

    public string DependencyName { get; }
}

class Dependency(int id, string dependencyName) : IDependency
{
    public int Id { get; } = id;

    public string DependencyName { get; } = dependencyName;
}

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service(
    [Tag(ForService)] string name,
    IDependency dependency)
    : IService
{
    public string Name { get; } = name;

    public IDependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

When using composition root arguments, compilation warnings are shown if `Resolve` methods are generated, since these methods will not be able to create these roots. You can disable the creation of `Resolve` methods using the `Hint(Hint.Resolve, "Off")` hint, or ignore them but remember the risks of using `Resolve` methods.

## Tags

Sometimes it's important to take control of building a dependency graph. For example, when there are different implementations of the same interface. In this case, _tags_ will help:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IDependency>("AbcTag", default).To<AbcDependency>()
    .Bind<IDependency>("XyzTag").As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // "XyzRoot" is root name, "XyzTag" is tag
    .Root<IDependency>("XyzRoot", "XyzTag")

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag("AbcTag")] IDependency dependency1,
    [Tag("XyzTag")] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Define multiple bindings for the same interface
- Use tags to differentiate between implementations
- Control lifetime management
- Inject tagged dependencies into constructors

The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. The _default_ and _null_ tags are also supported.

## Smart tags

When you have a large graph of objects, you may need a lot of tags to neatly define all the dependencies in it. Strings or other constant values are not always convenient to use, because they have too much variability. And there are often cases when you specify one tag in the binding, but the same tag in the dependency, but with a typo, which leads to a compilation error when checking the dependency graph. The solution to this problem is to create an `Enum` type and use its values as tags. Pure.DI makes it easier to solve this problem.

When you specify a tag in a binding and the compiler can't determine what that value is, Pure.DI will automatically create a constant for it inside the `Pure.DI.Tag` type. For the example below, the set of constants would look like this:

```c#
namespace Pure.DI
{
  internal partial class Tag
  {
    public const string Abc = "Abc";
    public const string Xyz = "Xyz";
  }
}
```
So you can apply refactoring in the development environment. And also tag changes in bindings will be automatically checked by the compiler. This will reduce the number of errors.

![](smart_tags.gif)

The example below also uses the `using static Pure.DI.Tag;` directive to access tags in `Pure.DI.Tag` without specifying a type name:

```c#
using Shouldly;
using Pure.DI;

using static Pure.DI.Tag;
using static Pure.DI.Lifetime;
        
DI.Setup(nameof(Composition))
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IDependency>(Abc, default).To<AbcDependency>()
    .Bind<IDependency>(Xyz).As(Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // "XyzRoot" is root name, Xyz is tag
    .Root<IDependency>("XyzRoot", Xyz)

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(Abc)] IDependency dependency1,
    [Tag(Xyz)] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Build up of an existing object

This example demonstrates the Build-Up pattern in dependency injection, where an existing object is injected with necessary dependencies through its properties, methods, or fields.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<string>("name")
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To(ctx =>
    {
        var dependency = new Dependency();
        ctx.BuildUp(dependency);
        return dependency;
    })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("GetMyService");

var composition = new Composition();
var service = composition.GetMyService("Some name");
service.Dependency.Name.ShouldBe("Some name");
service.Dependency.Id.ShouldNotBe(Guid.Empty);

interface IDependency
{
    string Name { get; }

    Guid Id { get; }
}

class Dependency : IDependency
{
    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public string Name { get; set; } = "";

    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public void SetId(Guid id) => Id = id;
}

interface IService
{
    IDependency Dependency { get; }
}

record Service(IDependency Dependency) : IService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Key Concepts:
**Build-Up** - injecting dependencies into an already created object
**Dependency Attribute** - marker for identifying injectable members

## Builder

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To<Dependency>()
    .Builder<Service>("BuildUpService");

var composition = new Composition();
        
var service = composition.BuildUpService(new Service());
service.Id.ShouldNotBe(Guid.Empty);
service.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }

    IDependency? Dependency { get; }
}

record Service: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built

Advantages:
- Allows working with pre-existing objects
- Provides flexibility in dependency injection
- Supports partial injection of dependencies
- Can be used with legacy code

Use Cases:
- When objects are created outside the DI container
- For working with third-party libraries
- When migrating existing code to DI
- For complex object graphs where full construction is not feasible

## Builder with arguments

This example demonstrates how to use builders with custom arguments in dependency injection. It shows how to pass additional parameters during the build-up process.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<Guid>("serviceId")
    .Bind().To<Dependency>()
    .Builder<Service>("BuildUpService");

var composition = new Composition();

var id = Guid.NewGuid();
var service = composition.BuildUpService(new Service(), id);
service.Id.ShouldBe(id);
service.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }

    IDependency? Dependency { get; }
}

record Service: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built
- Additional arguments are passed in the order they are defined in the setup
- Root arguments can be used to provide custom values during build-up

Use Cases:
- When additional parameters are required during object construction
- For scenarios where dependencies depend on runtime values
- When specific initialization data is needed
- For conditional injection based on provided arguments

Best Practices
- Keep the number of builder arguments minimal
- Use meaningful names for root arguments

## Builders

Sometimes you need builders for all types inherited from <see cref=“T”/> available at compile time at the point where the method is called.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To<Dependency>()
    // Creates a builder for each type inherited from IService.
    // These types must be available at this point in the code.
    .Builders<IService>("BuildUp");

var composition = new Composition();
        
var service1 = composition.BuildUp(new Service1());
service1.Id.ShouldNotBe(Guid.Empty);
service1.Dependency.ShouldBeOfType<Dependency>();

var service2 = composition.BuildUp(new Service2());
service2.Id.ShouldBe(Guid.Empty);
service2.Dependency.ShouldBeOfType<Dependency>();

// Uses a common method to build an instance
IService abstractService = new Service1();
abstractService = composition.BuildUp(abstractService);
abstractService.ShouldBeOfType<Service1>();
abstractService.Id.ShouldNotBe(Guid.Empty);
abstractService.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }

    IDependency? Dependency { get; }
}

record Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Service2 : IService
{
    public Guid Id => Guid.Empty;

    [Dependency]
    public IDependency? Dependency { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built

## Builders with a name template

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To<Dependency>()
    // Creates a builder based on the name template
    // for each type inherited from IService.
    // These types must be available at this point in the code.
    .Builders<IService>("BuildUp{type}");

var composition = new Composition();
        
var service1 = composition.BuildUpService1(new Service1());
service1.Id.ShouldNotBe(Guid.Empty);
service1.Dependency.ShouldBeOfType<Dependency>();

var service2 = composition.BuildUpService2(new Service2());
service2.Id.ShouldBe(Guid.Empty);
service2.Dependency.ShouldBeOfType<Dependency>();

// Uses a common method to build an instance
IService abstractService = new Service1();
abstractService = composition.BuildUpIService(abstractService);
abstractService.ShouldBeOfType<Service1>();
abstractService.Id.ShouldNotBe(Guid.Empty);
abstractService.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }

    IDependency? Dependency { get; }
}

record Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Service2 : IService
{
    public Guid Id => Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.

## Field injection

To use dependency injection for a field, make sure the field is writable and simply add the _Ordinal_ attribute to that field, specifying an ordinal that will be used to determine the injection order:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency? Dependency { get; }
}

class Service : IService
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency] public IDependency? DependencyVal;

    public IDependency? Dependency
    {
        get
        {
            return DependencyVal;
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The field must be writable
- The `Dependency` (or `Ordinal`) attribute is used to mark the field for injection
- The container automatically injects the dependency when resolving the object graph

## Method injection

To use dependency implementation for a method, simply add the _Ordinal_ attribute to that method, specifying the sequence number that will be used to define the call to that method:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency? Dependency { get; }
}

class Service : IService
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(IDependency dependency) =>
        Dependency = dependency;

    public IDependency? Dependency { get; private set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The method must be available to be called from a composition class
- The `Dependency` (or `Ordinal`) attribute is used to mark the method for injection
- The container automatically calls the method to inject dependencies

## Property injection

To use dependency injection on a property, make sure the property is writable and simply add the _Ordinal_ attribute to that property, specifying the ordinal that will be used to determine the injection order:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency? Dependency { get; }
}

class Service : IService
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency] public IDependency? Dependency { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The property must be writable
- The `Dependency` (or `Ordinal`) attribute is used to mark the property for injection
- The container automatically injects the dependency when resolving the object graph

## Default values

This example demonstrates how to use default values in dependency injection when explicit injection is not possible.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ShouldBeOfType<Dependency>();
service.Name.ShouldBe("My Service");

interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

// If injection cannot be performed explicitly,
// the default value will be used
class Service(string name = "My Service") : IService
{
    public string Name { get; } = name;

    public required IDependency Dependency { get; init; } = new Dependency();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- Default constructor arguments can be used for simple values
- The DI container will use these defaults if no explicit bindings are provided

This example illustrates how to handle default values in a dependency injection scenario:
- **Constructor Default Argument**: The `Service` class has a constructor with a default value for the name parameter. If no value is provided, “My Service” will be used.
- **Required Property with Default**: The Dependency property is marked as required but has a default instantiation. This ensures that:
  - The property must be set
  - If no explicit injection occurs, a default value will be used

## Required properties or fields

This example demonstrates how the `required` modifier can be used to automatically inject dependencies into properties and fields. When a property or field is marked with `required`, the DI will automatically inject the dependency without additional effort.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Arg<string>("name")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition(name: "My Service");
var service = composition.Root;
service.Dependency.ShouldBeOfType<Dependency>();
service.Name.ShouldBe("My Service");

interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service : IService
{
    public required string ServiceNameField;

    public string Name => ServiceNameField;

    // The required property will be injected automatically
    // without additional effort
    public required IDependency Dependency { get; init; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This approach simplifies dependency injection by eliminating the need to manually configure bindings for required dependencies, making the code more concise and easier to maintain.

## Overrides

This example demonstrates advanced dependency injection techniques using Pure.DI's override mechanism to customize dependency instantiation with runtime arguments and tagged parameters. The implementation creates multiple `IDependency` instances with values manipulated through explicit overrides.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;
using System.Drawing;

DI.Setup(nameof(Composition))
    .Bind(Tag.Red).To(_ => Color.Red)
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Func<int, int, IDependency>>(ctx =>
        (dependencyId, subId) =>
        {
            // Overrides with a lambda argument
            ctx.Override(dependencyId);

            // Overrides with tag using lambda argument
            ctx.Override(subId, "sub");

            // Overrides with some value
            ctx.Override($"Dep {dependencyId} {subId}");

            // Overrides with injected value
            ctx.Inject(Tag.Red, out Color red);
            ctx.Override(red);

            ctx.Inject<Dependency>(out var dependency);
            return dependency;
        })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

service.Dependencies[0].Id.ShouldBe(0);
service.Dependencies[0].SubId.ShouldBe(99);
service.Dependencies[0].Name.ShouldBe("Dep 0 99");

service.Dependencies[1].Id.ShouldBe(1);
service.Dependencies[1].Name.ShouldBe("Dep 1 99");

service.Dependencies[2].Id.ShouldBe(2);
service.Dependencies[2].Name.ShouldBe("Dep 2 99");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    string Name { get; }

    int Id { get; }

    int SubId { get; }
}

class Dependency(
    string name,
    IClock clock,
    int id,
    [Tag("sub")] int subId,
    Color red)
    : IDependency
{
    public string Name => name;

    public int Id => id;

    public int SubId => subId;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<int, int, IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(0, 99),
        dependencyFactory(1, 99),
        dependencyFactory(2, 99)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Root binding

In general, it is recommended to define one composition root for the entire application. But sometimes it is necessary to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It allows you to define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .RootBind<IService>("MyRoot").To<Service>();
// It's the same as:
//  .Bind<IService>().To<Service>()
//  .Root<IService>("MyRoot")

var composition = new Composition();
composition.MyRoot.ShouldBeOfType<Service>();

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Async Root

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Composition root
    .Root<Task<IService>>("GetMyServiceAsync");

var composition = new Composition();

// Resolves composition roots asynchronously
var service = await composition.GetMyServiceAsync(CancellationToken.None);

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Consumer type

`ConsumerType` is used to get the consumer type of the given dependency. The use of `ConsumerType` is demonstrated on the example of [Serilog library](https://serilog.net/):

```c#
using Shouldly;
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using Serilog.Core;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency
{
    public Dependency(Serilog.ILogger log)
    {
        log.Information("created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        Serilog.ILogger log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx =>
            {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);
                return logger.ForContext(ctx.ConsumerType);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)


## Ref dependencies

```c#
using Shouldly;
using Pure.DI;

DI.Setup("Composition")
    // Data
    .Bind().To<int[]>(_ => [ 1, 2, 3])
    .Root<Service>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Sum.ShouldBe(6);

class Service
{
    public int Sum { get; private set; }

    [Ordinal]
    public void Initialize(ref Data data) =>
        Sum = data.Sum();
}

readonly ref struct Data(ref int[] data)
{
    private readonly ref int[] _dep = ref data;

    public int Sum() => _dep.Sum();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Roots

Sometimes you need roots for all types inherited from <see cref="T"/> available at compile time at the point where the method is called.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .Roots<IService>("My{type}");

var composition = new Composition();
composition.MyService1.ShouldBeOfType<Service1>();
composition.MyService2.ShouldBeOfType<Service2>();

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service1(IDependency dependency) : IService;

class Service2(IDependency dependency) : IService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Roots with filter

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .Roots<IService>("My{type}", filter: "*2");

var composition = new Composition();
composition.MyService2.ShouldBeOfType<Service2>();

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service1(int dependency) : IService;

class Service2(IDependency dependency) : IService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Transient

The _Transient_ lifetime specifies to create a new dependency instance each time. It is the default lifetime and can be omitted.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(Transient).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.Dependency1.ShouldNotBe(service1.Dependency2);
service2.Dependency1.ShouldNotBe(service1.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The _Transient_ lifetime is the safest and is used by default. Yes, its widespread use can cause a lot of memory traffic, but if there are doubts about thread safety, the _Transient_ lifetime is preferable because each consumer has its own instance of the dependency. The following nuances should be considered when choosing the _Transient_ lifetime:

- There will be unnecessary memory overhead that could be avoided.

- Every object created must be disposed of, and this will waste CPU resources, at least when the GC does its memory-clearing job.

- Poorly designed constructors can run slowly, perform functions that are not their own, and greatly hinder the efficient creation of compositions of multiple objects.

> [!IMPORTANT]
> The following very important rule, in my opinion, will help in the last point. Now, when a constructor is used to implement dependencies, it should not be loaded with other tasks. Accordingly, constructors should be free of all logic except for checking arguments and saving them for later use. Following this rule, even the largest compositions of objects will be built quickly.

## Singleton

The _Singleton_ lifetime ensures that there will be a single instance of the dependency for each composition.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.Dependency1.ShouldBe(service1.Dependency2);
service2.Dependency1.ShouldBe(service1.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Some articles advise using objects with a _Singleton_ lifetime as often as possible, but the following details must be considered:

- For .NET the default behavior is to create a new instance of the type each time it is needed, other behavior requires, additional logic that is not free and requires additional resources.

- The use of _Singleton_, adds a requirement for thread-safety controls on their use, since singletons are more likely to share their state between different threads without even realizing it.

- The thread-safety control should be automatically extended to all dependencies that _Singleton_ uses, since their state is also now shared.

- Logic for thread-safety control can be resource-costly, error-prone, interlocking, and difficult to test.

- _Singleton_ can retain dependency references longer than their expected lifetime, this is especially significant for objects that hold "non-renewable" resources, such as the operating system Handler.

- Sometimes additional logic is required to dispose of _Singleton_.

## PerResolve

The _PerResolve_ lifetime ensures that there will be one instance of the dependency for each composition root instance.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(PerResolve).To<Dependency>()
    .Bind().As(Singleton).To<(IDependency dep3, IDependency dep4)>()

    // Composition root
    .Root<Service>("Root");

var composition = new Composition();

var service1 = composition.Root;
service1.Dep1.ShouldBe(service1.Dep2);
service1.Dep3.ShouldBe(service1.Dep4);
service1.Dep1.ShouldBe(service1.Dep3);

var service2 = composition.Root;
service2.Dep1.ShouldNotBe(service1.Dep1);

interface IDependency;

class Dependency : IDependency;

class Service(
    IDependency dep1,
    IDependency dep2,
    (IDependency dep3, IDependency dep4) deps)
{
    public IDependency Dep1 { get; } = dep1;

    public IDependency Dep2 { get; } = dep2;

    public IDependency Dep3 { get; } = deps.dep3;

    public IDependency Dep4 { get; } = deps.dep4;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## PerBlock

The _PreBlock_ lifetime does not guarantee that there will be a single dependency instance for each instance of the composition root (as for the _PreResolve_ lifetime), but is useful for reducing the number of instances of a type.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(PerBlock).To<Dependency>()
    .Bind().As(Singleton).To<(IDependency dep3, IDependency dep4)>()

    // Composition root
    .Root<Service>("Root");

var composition = new Composition();

var service1 = composition.Root;
service1.Dep1.ShouldBe(service1.Dep2);
service1.Dep3.ShouldBe(service1.Dep4);
service1.Dep1.ShouldBe(service1.Dep3);

var service2 = composition.Root;
service2.Dep1.ShouldNotBe(service1.Dep1);

interface IDependency;

class Dependency : IDependency;

class Service(
    IDependency dep1,
    IDependency dep2,
    (IDependency dep3, IDependency dep4) deps)
{
    public IDependency Dep1 { get; } = dep1;

    public IDependency Dep2 { get; } = dep2;

    public IDependency Dep3 { get; } = deps.dep3;

    public IDependency Dep4 { get; } = deps.dep4;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Scope

The _Scoped_ lifetime ensures that there will be a single instance of the dependency for each scope.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var program = composition.ProgramRoot;

// Creates session #1
var session1 = program.CreateSession();
var dependency1 = session1.SessionRoot.Dependency;
var dependency12 = session1.SessionRoot.Dependency;

// Checks the identity of scoped instances in the same session
dependency1.ShouldBe(dependency12);

// Creates session #2
var session2 = program.CreateSession();
var dependency2 = session2.SessionRoot.Dependency;

// Checks that the scoped instances are not identical in different sessions
dependency1.ShouldNotBe(dependency2);

// Disposes of session #1
session1.Dispose();
// Checks that the scoped instance is finalized
dependency1.IsDisposed.ShouldBeTrue();

// Disposes of session #2
session2.Dispose();
// Checks that the scoped instance is finalized
dependency2.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition parent) : Composition(parent);

partial class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().As(Scoped).To<Dependency>()
            .Bind().To<Service>()

            // Session composition root
            .Root<IService>("SessionRoot")

            // Composition root
            .Root<Program>("ProgramRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Auto scoped

You can use the following example to automatically create a session when creating instances of a particular type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var program = composition.ProgramRoot;

// Creates service in session #1
var service1 = program.CreateService();

// Creates service in session #2
var service2 = program.CreateService();

// Checks that the scoped instances are not identical in different sessions
service1.Dependency.ShouldNotBe(service2.Dependency);

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
            .Bind().As(Scoped).To<Dependency>()
            // Session composition root
            .Root<Service>("SessionRoot", kind: RootKinds.Private)
            // Auto scoped
            .Bind().To(IService (Composition parentScope) =>
            {
                // Creates a new scope from the parent scope
                var scope = new Composition(parentScope);
                // Provides the session root in a new scope
                return scope.SessionRoot;
            })

            // Composition root
            .Root<Program>("ProgramRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Default lifetime

For example, if some lifetime is used more often than others, you can make it the default lifetime:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Default Lifetime applies
    // to all bindings until the end of the chain
    // or the next call to the DefaultLifetime method
    .DefaultLifetime(Singleton)
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldBe(service2);
service1.Dependency1.ShouldBe(service1.Dependency2);
service1.Dependency1.ShouldBe(service2.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Default lifetime for a type

For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Default lifetime applied to a specific type
    .DefaultLifetime<IDependency>(Singleton)
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldNotBe(service2);
service1.Dependency1.ShouldBe(service1.Dependency2);
service1.Dependency1.ShouldBe(service2.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Default lifetime for a type and a tag

For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Default lifetime applied to a specific type
    .DefaultLifetime<IDependency>(Singleton, "some tag")
    .Bind("some tag").To<Dependency>()
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldNotBe(service2);
service1.Dependency1.ShouldNotBe(service1.Dependency2);
service1.Dependency1.ShouldBe(service2.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    [Tag("some tag")] IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Disposable singleton

To dispose all created singleton instances, simply dispose the composition instance:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().As(Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

IDependency dependency;
using (var composition = new Composition())
{
    var service = composition.Root;
    dependency = service.Dependency;
}

dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

A composition class becomes disposable if it creates at least one disposable singleton instance.

## Async disposable singleton

If at least one of these objects implements the `IAsyncDisposable` interface, then the composition implements `IAsyncDisposable` as well. To dispose of all created singleton instances in an asynchronous manner, simply dispose of the composition instance in an asynchronous manner:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

IDependency dependency;
await using (var composition = new Composition())
{
    var service = composition.Root;
    dependency = service.Dependency;
}

dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Async disposable scope

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var program = composition.ProgramRoot;

// Creates session #1
var session1 = program.CreateSession();
var dependency1 = session1.SessionRoot.Dependency;
var dependency12 = session1.SessionRoot.Dependency;

// Checks the identity of scoped instances in the same session
dependency1.ShouldBe(dependency12);

// Creates session #2
var session2 = program.CreateSession();
var dependency2 = session2.SessionRoot.Dependency;

// Checks that the scoped instances are not identical in different sessions
dependency1.ShouldNotBe(dependency2);

// Disposes of session #1
await session1.DisposeAsync();
// Checks that the scoped instance is finalized
dependency1.IsDisposed.ShouldBeTrue();

// Disposes of session #2
await session2.DisposeAsync();
// Checks that the scoped instance is finalized
dependency2.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition composition) : Composition(composition);

partial class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().As(Scoped).To<Dependency>()
            .Bind().To<Service>()

            // Session composition root
            .Root<IService>("SessionRoot")

            // Program composition root
            .Root<Program>("ProgramRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Func

_Func<T>_ helps when the logic must enter instances of some type on demand or more than once. This is a very handy mechanism for instance replication. For example it is used when implementing the `Lazy<T>` injection.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies =>
    [
        dependencyFactory(),
        dependencyFactory(),
        dependencyFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Be careful, replication takes into account the lifetime of the object.

## Enumerable

Specifying `IEnumerable<T>` as the injection type allows you to inject instances of all bindings that implement type `T` in a lazy fashion - the instances will be provided one by one, in order corresponding to the sequence of bindings.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<AbcDependency>()
    .Bind<IDependency>(2).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(2);
service.Dependencies[0].ShouldBeOfType<AbcDependency>();
service.Dependencies[1].ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(IEnumerable<IDependency> dependencies) : IService
{
    public ImmutableArray<IDependency> Dependencies { get; }
        = [..dependencies];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Enumerable generics

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency<TT>>().To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>("Xyz").To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition roots
    .Root<IService<int>>("IntRoot")
    .Root<IService<string>>("StringRoot");

var composition = new Composition();

var intService = composition.IntRoot;
intService.Dependencies.Length.ShouldBe(2);
intService.Dependencies[0].ShouldBeOfType<AbcDependency<int>>();
intService.Dependencies[1].ShouldBeOfType<XyzDependency<int>>();

var stringService = composition.StringRoot;
stringService.Dependencies.Length.ShouldBe(2);
stringService.Dependencies[0].ShouldBeOfType<AbcDependency<string>>();
stringService.Dependencies[1].ShouldBeOfType<XyzDependency<string>>();

interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = [..dependencies];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Array

Specifying `T[]` as the injection type allows instances from all bindings that implement the `T` type to be injected.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<AbcDependency>()
    .Bind<IDependency>(2).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(2);
service.Dependencies[0].ShouldBeOfType<AbcDependency>();
service.Dependencies[1].ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency[] Dependencies { get; }
}

class Service(IDependency[] dependencies) : IService
{
    public IDependency[] Dependencies { get; } = dependencies;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

In addition to arrays, other collection types are also supported, such as:
- System.Memory<T>
- System.ReadOnlyMemory<T>
- System.Span<T>
- System.ReadOnlySpan<T>
- System.Collections.Generic.ICollection<T>
- System.Collections.Generic.IList<T>
- System.Collections.Generic.List<T>
- System.Collections.Generic.IReadOnlyCollection<T>
- System.Collections.Generic.IReadOnlyList<T>
- System.Collections.Generic.ISet<T>
- System.Collections.Generic.HashSet<T>
- System.Collections.Generic.SortedSet<T>
- System.Collections.Generic.Queue<T>
- System.Collections.Generic.Stack<T>
- System.Collections.Immutable.ImmutableArray<T>
- System.Collections.Immutable.IImmutableList<T>
- System.Collections.Immutable.ImmutableList<T>
- System.Collections.Immutable.IImmutableSet<T>
- System.Collections.Immutable.ImmutableHashSet<T>
- System.Collections.Immutable.ImmutableSortedSet<T>
- System.Collections.Immutable.IImmutableQueue<T>
- System.Collections.Immutable.ImmutableQueue<T>
- System.Collections.Immutable.IImmutableStack<T>
And of course this list can easily be supplemented on its own.

## Lazy

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ShouldBe(service.Dependency);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(Lazy<IDependency> dependency) : IService
{
    public IDependency Dependency => dependency.Value;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Task

By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:

- CancellationToken.None
- TaskScheduler.Default
- TaskCreationOptions.None
- TaskContinuationOptions.None

But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Overrides TaskScheduler.Default if necessary
    .Bind<TaskScheduler>().To(_ => TaskScheduler.Current)
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("GetRoot");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var service = composition.GetRoot(cancellationTokenSource.Token);
await service.RunAsync(cancellationTokenSource.Token);

interface IDependency
{
    ValueTask DoSomething(CancellationToken cancellationToken);
}

class Dependency : IDependency
{
    public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IService
{
    Task RunAsync(CancellationToken cancellationToken);
}

class Service(Task<IDependency> dependencyTask) : IService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## ValueTask

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
await service.RunAsync();

interface IDependency
{
    ValueTask DoSomething();
}

class Dependency : IDependency
{
    public ValueTask DoSomething() => ValueTask.CompletedTask;
}

interface IService
{
    ValueTask RunAsync();
}

class Service(ValueTask<IDependency> dependencyTask) : IService
{
    public async ValueTask RunAsync()
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething();
    }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Manually started tasks

By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Overrides the default binding that performs an auto-start of a task
    // when it is created. This binding will simply create the task.
    // The start will be handled by the consumer.
    .Bind<Task<TT>>().To(ctx =>
    {
        ctx.Inject(ctx.Tag, out Func<TT> factory);
        ctx.Inject(out CancellationToken cancellationToken);
        return new Task<TT>(factory, cancellationToken);
    })
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("GetRoot");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var service = composition.GetRoot(cancellationTokenSource.Token);
await service.RunAsync(cancellationTokenSource.Token);

interface IDependency
{
    ValueTask DoSomething(CancellationToken cancellationToken);
}

class Dependency : IDependency
{
    public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IService
{
    Task RunAsync(CancellationToken cancellationToken);
}

class Service : IService
{
    private readonly Task<IDependency> _dependencyTask;

    public Service(Task<IDependency> dependencyTask)
    {
        _dependencyTask = dependencyTask;
        // This is where the task starts
        _dependencyTask.Start();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await _dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Span and ReadOnlySpan

Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<Dependency>('a').To<Dependency>()
    .Bind<Dependency>('b').To<Dependency>()
    .Bind<Dependency>('c').To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Count.ShouldBe(3);

struct Dependency;

interface IService
{
    int Count { get; }
}

class Service(ReadOnlySpan<Dependency> dependencies) : IService
{
    public int Count { get; } = dependencies.Length;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This scenario is even more efficient in the case of `Span<T>` or `ReadOnlySpan<T>` when `T` is a value type. In this case, there is no heap allocation, and the composition root `IService` looks like this:
```c#
public IService Root
{
  get
  {
    ReadOnlySpan<Dependency> dependencies = stackalloc Dependency[3] { new Dependency(), new Dependency(), new Dependency() };
    return new Service(dependencies);
  }
}
```

## Tuple

The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<Point>().To(_ => new Point(7, 9))
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var root = composition.Root;

interface IDependency;

class Dependency : IDependency;

readonly record struct Point(int X, int Y);

interface IService
{
    IDependency Dependency { get; }
}

class Service((Point Point, IDependency Dependency) tuple) : IService
{
    public IDependency Dependency { get; } = tuple.Dependency;
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Weak Reference

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(WeakReference<IDependency> dependency) : IService
{
    public IDependency? Dependency =>
        dependency.TryGetTarget(out var value)
            ? value
            : null;
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Async Enumerable

Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<AbcDependency>()
    .Bind<IDependency>(2).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = await service.GetDependenciesAsync();
dependencies[0].ShouldBeOfType<AbcDependency>();
dependencies[1].ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    Task<IReadOnlyList<IDependency>> GetDependenciesAsync();
}

class Service(IAsyncEnumerable<IDependency> dependencies) : IService
{
    public async Task<IReadOnlyList<IDependency>> GetDependenciesAsync()
    {
        var deps = new List<IDependency>();
        await foreach (var dependency in dependencies)
        {
            deps.Add(dependency);
        }

        return deps;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Service collection

The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an _IServiceCollection_ object:

```c#
using Pure.DI.MS;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var composition = new Composition();
var serviceCollection = composition.ServiceCollection;
var serviceProvider = serviceCollection.BuildServiceProvider();
var service = serviceProvider.GetRequiredService<IService>();
var dependency = serviceProvider.GetRequiredKeyedService<IDependency>("Dependency Key");
service.Dependency.ShouldBe(dependency);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service([Tag("Dependency Key")] IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition : ServiceProviderFactory<Composition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    static void Setup() =>
        DI.Setup()
            .Bind<IDependency>("Dependency Key").As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>(tag: "Dependency Key")
            .Root<IService>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Pure.DI.MS](https://www.nuget.org/packages/Pure.DI.MS)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)


## Func with arguments

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

service.Dependencies[0].Name.ShouldBe("Abc");
service.Dependencies[0].Id.ShouldBe(0);

service.Dependencies[1].Name.ShouldBe("Xyz");
service.Dependencies[1].Id.ShouldBe(1);

service.Dependencies[2].Id.ShouldBe(2);
service.Dependencies[2].Name.ShouldBe("");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    string Name { get; }
    int Id { get; }
}

class Dependency(string name, IClock clock, int id)
    : IDependency
{
    public string Name => name;
    public int Id => id;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<int, string, IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(0, "Abc"),
        dependencyFactory(1, "Xyz"),
        dependencyFactory(2, "")
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Func with tag

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency>("my tag").To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service([Tag("my tag")] Func<IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory(),
        dependencyFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Keyed service provider

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var serviceProvider = new Composition();
var service = serviceProvider.GetRequiredKeyedService<IService>("Service Key");
var dependency = serviceProvider.GetRequiredKeyedService<IDependency>("Dependency Key");
service.Dependency.ShouldBe(dependency);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service([Tag("Dependency Key")] IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition : IKeyedServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IDependency>("Dependency Key").As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>("Service Key").To<Service>()
            .Root<IDependency>(tag: "Dependency Key")
            .Root<IService>(tag: "Service Key");

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)


## Service provider

The `// ObjectResolveMethodName = GetService` hint overriding the `object Resolve(Type type)` method name in `GetService()`, allowing the `IServiceProvider` interface to be implemented in a partial class.
> [!IMPORTANT]
> Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.

This example demonstrates how to implement a custom `IServiceProvider` using a partial class, utilizing a specific hint to override the default `Resolve()` method name:

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var serviceProvider = new Composition();
var service = serviceProvider.GetRequiredService<IService>();
var dependency = serviceProvider.GetRequiredService<IDependency>();
service.Dependency.ShouldBe(dependency);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition : IServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

Important Notes:
- Hint Overriding: The `ObjectResolveMethodName = GetService` hint overrides the default object `Resolve(Type type)` method name to implement `IServiceProvider` interface
- Roots: Only roots can be resolved. Use `Root(...)` or `RootBind()` calls for registration

## Service provider with scope

> [!IMPORTANT]
> Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

using var composition = new Composition();

using var scope1 = composition.CreateScope();
var service1 = scope1.ServiceProvider.GetRequiredService<IService>();
var dependency1 = composition.GetRequiredService<IDependency>();
service1.Dependency.ShouldBe(dependency1);
service1.ShouldBe(scope1.ServiceProvider.GetRequiredService<IService>());

using var scope2 = composition.CreateScope();
var service2 = scope2.ServiceProvider.GetRequiredService<IService>();
var dependency2 = composition.GetRequiredService<IDependency>();
service2.Dependency.ShouldBe(dependency2);
service2.ShouldBe(scope2.ServiceProvider.GetRequiredService<IService>());

service1.ShouldNotBe(service2);
dependency1.ShouldBe(dependency2);

interface IDependency;

class Dependency : IDependency;

interface IService : IDisposable
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public void Dispose()
    {
    }
}

partial class Composition
    : IKeyedServiceProvider, IServiceScopeFactory, IServiceScope
{
    static void Setup() =>
        // The following hint overrides the name of the
        // "object Resolve(Type type)" method in "GetService",
        // which implements the "IServiceProvider" interface:
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().As(Lifetime.Scoped).To<Service>()

            // Composition roots
            .Root<IDependency>()
            .Root<IService>();

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => new Composition(this);

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)


## Overriding the BCL binding

At any time, the default binding to the BCL type can be changed to your own:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency[]>().To<IDependency[]>(_ =>
        [new AbcDependency(), new XyzDependency(), new AbcDependency()]
    )
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);
service.Dependencies[0].ShouldBeOfType<AbcDependency>();
service.Dependencies[1].ShouldBeOfType<XyzDependency>();
service.Dependencies[2].ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency[] Dependencies { get; }
}

class Service(IDependency[] dependencies) : IService
{
    public IDependency[] Dependencies { get; } = dependencies;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Generics

Generic types are also supported.
> [!IMPORTANT]
> Instead of open generic types, as in classical DI container libraries, regular generic types with _marker_ types as type parameters are used here. Such "marker" types allow to define dependency graph more precisely.

For the case of `IDependency<TT>`, `TT` is a _marker_ type, which allows the usual `IDependency<TT>` to be used instead of an open generic type like `IDependency<>`. This makes it easy to bind generic types by specifying _marker_ types such as `TT`, `TT1`, etc. as parameters of generic types:

```c#
using Shouldly;
using Pure.DI;

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
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Actually, the property _Root_ looks like:
```c#
public IService Root
{
  get
  {
    return new Service(new Dependency<int>(), new Dependency<string>());
  }
}
```
Even in this simple scenario, it is not possible to precisely define the binding of an abstraction to its implementation using open generic types:
```c#
.Bind(typeof(IMap<,>)).To(typeof(Map<,>))
```
You can try to match them by order or by name derived from the .NET type reflection. But this is not reliable, since order and name matching is not guaranteed. For example, there is some interface with two arguments of type _key and _value_. But in its implementation the sequence of type arguments is mixed up: first comes the _value_ and then the _key_ and the names do not match:
```c#
class Map<TV, TK>: IMap<TKey, TValue> { }
```
At the same time, the marker types `TT1` and `TT2` handle this easily. They determine the exact correspondence between the type arguments in the interface and its implementation:
```c#
.Bind<IMap<TT1, TT2>>().To<Map<TT2, TT1>>()
```
The first argument of the type in the interface, corresponds to the second argument of the type in the implementation and is a _key_. The second argument of the type in the interface, corresponds to the first argument of the type in the implementation and is a _value_. This is a simple example. Obviously, there are plenty of more complex scenarios where tokenized types will be useful.
Marker types are regular .NET types marked with a special attribute, such as:
```c#
[GenericTypeArgument]
internal abstract class TT1 { }

[GenericTypeArgument]
internal abstract class TT2 { }
```
This way you can easily create your own, including making them fit the constraints on the type parameter, for example:
```c#
[GenericTypeArgument]
internal struct TTS { }

[GenericTypeArgument]
internal interface TTDisposable: IDisposable { }

[GenericTypeArgument]
internal interface TTEnumerator<out T>: IEnumerator<T> { }
```

## Generic composition roots

Sometimes you want to be able to create composition roots with type parameters. In this case, the composition root can only be represented by a method.
> [!IMPORTANT]
> `Resolve()' methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T>
    // with the name "GetMyRoot"
    .Root<IService<TT>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TT>>("GetOtherService", "Other");

var composition = new Composition();

// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyRoot<int>();

// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetOtherService<string>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Complex generics

Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ```IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }``` and its binding to the some implementation ```.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .RootArg<TT>("depArg")
    .Bind<IDependency<TT>>().To<Dependency<TT>>()
    .Bind<IDependency<TTS>>("value type")
        .As(Lifetime.Singleton)
        .To<DependencyStruct<TTS>>()
    .Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
        .To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()

    // Composition root
    .Root<Program<TT>>("GetRoot");

var composition = new Composition();
var program = composition.GetRoot<string>(depArg: "some value");
var service = program.Service;
service.ShouldBeOfType<Service<string, int, List<string>, Dictionary<string, int>>>();
service.Dependency1.ShouldBeOfType<Dependency<string>>();
service.Dependency2.ShouldBeOfType<DependencyStruct<int>>();

interface IDependency<T>;

class Dependency<T>(T value) : IDependency<T>;

readonly record struct DependencyStruct<T> : IDependency<T>
    where T : struct;

interface IService<T1, T2, TList, TDictionary>
    where T2 : struct
    where TList : IList<T1>
    where TDictionary : IDictionary<T1, T2>
{
    IDependency<T1> Dependency1 { get; }

    IDependency<T2> Dependency2 { get; }
}

class Service<T1, T2, TList, TDictionary>(
    IDependency<T1> dependency1,
    [Tag("value type")] IDependency<T2> dependency2)
    : IService<T1, T2, TList, TDictionary>
    where T2 : struct
    where TList : IList<T1>
    where TDictionary : IDictionary<T1, T2>
{
    public IDependency<T1> Dependency1 { get; } = dependency1;

    public IDependency<T2> Dependency2 { get; } = dependency2;
}

class Program<T>(IService<T, int, List<T>, Dictionary<T, int>> service)
    where T : notnull
{
    public IService<T, int, List<T>, Dictionary<T, int>> Service { get; } = service;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.

## Generic composition roots with constraints

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TTDisposable>>()
    .Bind().To<Service<TTDisposable, TTS>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TTDisposable> dependency);
        return new OtherService<TTDisposable>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T, TStruct>
    // with the name "GetMyRoot"
    .Root<IService<TTDisposable, TTS>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TTDisposable, bool>>("GetOtherService", "Other");

var composition = new Composition();

// service = new Service<Stream, double>(new Dependency<Stream>());
var service = composition.GetMyRoot<Stream, double>();

// someOtherService = new OtherService<BinaryReader>(new Dependency<BinaryReader>());
var someOtherService = composition.GetOtherService<BinaryReader>();

interface IDependency<T>
    where T : IDisposable;

class Dependency<T> : IDependency<T>
    where T : IDisposable;

interface IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T : IDisposable;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Generic async composition roots with constraints

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TTDisposable>>()
    .Bind().To<Service<TTDisposable, TTS>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TTDisposable> dependency);
        return new OtherService<TTDisposable>(dependency);
    })

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Specifies to create a regular public method
    // to get a composition root of type Task<Service<T, TStruct>>
    // with the name "GetMyRootAsync"
    .Root<Task<IService<TTDisposable, TTS>>>("GetMyRootAsync")

    // Specifies to create a regular public method
    // to get a composition root of type Task<OtherService<T>>
    // with the name "GetOtherServiceAsync"
    // using the "Other" tag
    .Root<Task<IService<TTDisposable, bool>>>("GetOtherServiceAsync", "Other");

var composition = new Composition();

// Resolves composition roots asynchronously
var service = await composition.GetMyRootAsync<Stream, double>(CancellationToken.None);
var someOtherService = await composition.GetOtherServiceAsync<BinaryReader>(CancellationToken.None);

interface IDependency<T>
    where T : IDisposable;

class Dependency<T> : IDependency<T>
    where T : IDisposable;

interface IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T : IDisposable;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

## Custom generic argument

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Registers custom generic argument
    .GenericTypeArgument<TTMy>()
    .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.IntDependency.ShouldBeOfType<Dependency<int>>();
service.StringDependency.ShouldBeOfType<Dependency<string>>();

interface TTMy;

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
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Build up of an existing generic object

In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<string>("name")
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To(ctx =>
    {
        var dependency = new Dependency<TTS>();
        ctx.BuildUp(dependency);
        return dependency;
    })
    .Bind().To<Service<TTS>>()

    // Composition root
    .Root<IService<Guid>>("GetMyService");

var composition = new Composition();
var service = composition.GetMyService("Some name");
service.Dependency.Name.ShouldBe("Some name");
service.Dependency.Id.ShouldNotBe(Guid.Empty);

interface IDependency<out T>
    where T: struct
{
    string Name { get; }

    T Id { get; }
}

class Dependency<T> : IDependency<T>
    where T: struct
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public string Name { get; set; } = "";

    public T Id { get; private set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(T id) => Id = id;
}

interface IService<out T>
    where T: struct
{
    IDependency<T> Dependency { get; }
}

record Service<T>(IDependency<T> Dependency)
    : IService<T> where T: struct;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Generic root arguments

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<TT>("someArg")
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<TT>>("GetMyService");

var composition = new Composition();
IService<int> service = composition.GetMyService<int>(someArg: 33);

interface IService<out T>
{
    T? Dependency { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(T dependency) =>
        Dependency = dependency;

    public T? Dependency { get; private set; }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Complex generic root arguments

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<MyData<TT>>("complexArg")
    .Bind<IService<TT2>>().To<Service<TT2>>()

    // Composition root
    .Root<IService<TT3>>("GetMyService");

var composition = new Composition();
IService<int> service = composition.GetMyService<int>(
    new MyData<int>(33, "Just contains an integer value 33"));

record MyData<T>(T Value, string Description);

interface IService<out T>
{
    T? Val { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(MyData<T> data) =>
        Val = data.Value;

    public T? Val { get; private set; }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Generic builder

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
    .Bind().To<Dependency<TT>>()
    // Generic service builder
    .Builder<Service<TTS, TT2>>("BuildUpGeneric");

var composition = new Composition();
var service = composition.BuildUpGeneric(new Service<Guid, string>());
service.Id.ShouldNotBe(Guid.Empty);
service.Dependency.ShouldBeOfType<Dependency<string>>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<out T, T2>
{
    T Id { get; }

    IDependency<T2>? Dependency { get; }
}

record Service<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Generic builders

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
    .Bind().To<Dependency<TT>>()
    // Generic service builder
    .Builders<IService<TT, TT2>>("BuildUpGeneric");

var composition = new Composition();

var service1 = composition.BuildUpGeneric(new Service1<Guid, string>());
service1.Id.ShouldNotBe(Guid.Empty);
service1.Dependency.ShouldBeOfType<Dependency<string>>();

var service2 = composition.BuildUpGeneric(new Service2<Guid, int>());
service2.Id.ShouldBe(Guid.Empty);
service2.Dependency.ShouldBeOfType<Dependency<int>>();

// Uses a common method to build an instance
IService<Guid, Uri> abstractService = new Service1<Guid, Uri>();
abstractService = composition.BuildUpGeneric(abstractService);
abstractService.ShouldBeOfType<Service1<Guid, Uri>>();
abstractService.Id.ShouldNotBe(Guid.Empty);
abstractService.Dependency.ShouldBeOfType<Dependency<Uri>>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<out T, T2>
{
    T Id { get; }

    IDependency<T2>? Dependency { get; }
}

record Service1<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
}

record Service2<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Generic roots

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind<OtherService<TT>>().To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to define composition roots for all types inherited from IService<TT>
    // available at compile time at the point where the method is called
    .Roots<IService<TT>>("GetMy{type}");

var composition = new Composition();

// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyService_T<int>();

// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetMyOtherService_T<string>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Generic injections as required

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()

    // Composition root
    .Root<IService<int>>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Count.ShouldBe(2);

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>
{
    IReadOnlyList<IDependency<T>> Dependencies { get; }
}

class Service<T>(Func<IDependency<T>> dependencyFactory): IService<T>
{
    public IReadOnlyList<IDependency<T>> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Generic injections as required with arguments

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = service.Dependencies;
dependencies.Count.ShouldBe(2);
dependencies[0].Id.ShouldBe(33);
dependencies[1].Id.ShouldBe(99);

interface IDependency<out T>
{
    int Id { get; }
}

class Dependency<T>(int id) : IDependency<T>
{
    public int Id { get; } = id;
}

interface IService<out T>
{
    IReadOnlyList<IDependency<T>> Dependencies { get; }
}

class Service<T>(Func<int, IDependency<T>> dependencyFactoryWithArgs): IService<T>
{
    public IReadOnlyList<IDependency<T>> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Constructor ordinal attribute

When applied to any constructor in a type, automatic injection constructor selection is disabled. The selection will only focus on constructors marked with this attribute, in the appropriate order from smallest value to largest.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Arg<string>("serviceName")
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition(serviceName: "Xyz");
var service = composition.Root;
service.ToString().ShouldBe("Xyz");

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    private readonly string _name;

    // The integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(1)]
    public Service(IDependency dependency) =>
        _name = "with dependency";

    [Ordinal(0)]
    internal Service(string name) => _name = name;

    public Service() => _name = "default";

    public override string ToString() => _name;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

## Dependency attribute

When applied to a property or field, these type members will also participate in dependency injection in the appropriate order from smallest value to largest.

```c#
using Shouldly;
using Pure.DI;
using System.Text;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Arg<string>("personName")
    .Arg<DateTime>("personBirthday")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(
    personId: 123,
    personName: "Nik",
    personBirthday: new DateTime(1977, 11, 16));

var person = composition.Person;
person.Name.ShouldBe("123 Nik 1977-11-16");

interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    [Dependency] public int Id;

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency(ordinal: 1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    [Dependency(ordinal: 2)]
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The attribute `Dependency` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

## Member ordinal attribute

When applied to a property or field, these type members will also participate in dependency injection in the appropriate order from smallest value to largest.

```c#
using Shouldly;
using Pure.DI;
using System.Text;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Arg<string>("personName")
    .Arg<DateTime>("personBirthday")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(
    personId: 123,
    personName: "Nik",
    personBirthday: new DateTime(1977, 11, 16));

var person = composition.Person;
person.Name.ShouldBe("123 Nik 1977-11-16");

interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(0)] public int Id;

    [Ordinal(1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    [Ordinal(2)]
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

## Tag attribute

Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, _tags_ will help:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("Abc").To<AbcDependency>()
    .Bind("Xyz").To<XyzDependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    [Tag("Abc")] IDependency dependency1,
    [Tag("Xyz")] IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

## Type attribute

The injection type can be defined manually using the `Type` attribute. This attribute explicitly overrides an injected type, otherwise it would be determined automatically based on the type of the constructor/method, property, or field parameter.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    [Type(typeof(AbcDependency))] IDependency dependency1,
    [Type(typeof(XyzDependency))] IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

## Inject attribute

If you want to use attributes in your libraries but don't want to create your own, you can add this package to your projects:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.

```c#
using Shouldly;
using Pure.DI.Abstractions;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Bind<Uri>("Person Uri").To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(_ => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

interface IPerson;

class Person([Inject("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>] internal object Id = "";

    public void Initialize([Inject<Uri>("Person Uri", 1)] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Custom attributes

It's very easy to use your attributes. To do this, you need to create a descendant of the `System.Attribute` class and register it using one of the appropriate methods:
- `TagAttribute`
- `OrdinalAttribute`
- `TagAttribute`
You can also use combined attributes, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .TagAttribute<MyTagAttribute>()
    .OrdinalAttribute<MyOrdinalAttribute>()
    .TypeAttribute<MyTypeAttribute>()
    .TypeAttribute<MyGenericTypeAttribute<TT>>()
    .Arg<int>("personId")
    .Bind().To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(_ => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field)]
class MyOrdinalAttribute(int ordinal) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTagAttribute(object tag) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTypeAttribute(Type type) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyGenericTypeAttribute<T> : Attribute;

interface IPerson;

class Person([MyTag("NikName")] string name) : IPerson
{
    private object? _state;

    [MyOrdinal(1)] [MyType(typeof(int))] internal object Id = "";

    [MyOrdinal(2)]
    public void Initialize([MyGenericType<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Custom universal attribute

You can use a combined attribute, and each method in the list above has an optional parameter that defines the argument number (the default is 0) from where to get the appropriate metadata for _tag_, _ordinal_, or _type_.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .TagAttribute<InjectAttribute<TT>>()
    .OrdinalAttribute<InjectAttribute<TT>>(1)
    .TypeAttribute<InjectAttribute<TT>>()
    .Arg<int>("personId")
    .Bind().To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(_ => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class InjectAttribute<T>(object? tag = null, int ordinal = 0) : Attribute;

interface IPerson;

class Person([Inject<string>("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>(ordinal: 1)] internal object Id = "";

    public void Initialize([Inject<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Custom generic argument attribute

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Registers custom generic argument
    .GenericTypeArgumentAttribute<MyGenericTypeArgumentAttribute>()
    .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.IntDependency.ShouldBeOfType<Dependency<int>>();
service.StringDependency.ShouldBeOfType<Dependency<string>>();

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
class MyGenericTypeArgumentAttribute : Attribute;

[MyGenericTypeArgument]
interface TTMy;

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
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Bind attribute

`BindAttribute` allows you to perform automatic binding to properties, fields or methods that belong to the type of the binding involved.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();

interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

This attribute `BindAttribute` applies to field properties and methods, to regular, static, and even returning generalized types.

## Bind attribute with lifetime and tag

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();

interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind(lifetime: Lifetime.Singleton, tags: ["my tag"])]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service([Tag("my tag")] IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Bind attribute for a generic type

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();

interface IDependency<T>
{
    public void DoSomething();
}

class Dependency<T> : IDependency<T>
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind(typeof(IDependency<TT>))]
    public IDependency<T> GetDependency<T>() => new Dependency<T>();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency<int> dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Decorator

Interception is the ability to intercept calls between objects in order to enrich or change their behavior, but without having to change their code. A prerequisite for interception is weak binding. That is, if programming is abstraction-based, the underlying implementation can be transformed or improved by "packaging" it into other implementations of the same abstraction. At its core, intercept is an application of the Decorator design pattern. This pattern provides a flexible alternative to inheritance by dynamically "attaching" additional responsibility to an object. Decorator "packs" one implementation of an abstraction into another implementation of the same abstraction like a "matryoshka doll".
_Decorator_ is a well-known and useful design pattern. It is convenient to use tagged dependencies to build a chain of nested decorators, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("base").To<Service>()
    .Bind().To<GreetingService>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.GetMessage().ShouldBe("Hello World !!!");

interface IService
{
    string GetMessage();
}

class Service : IService
{
    public string GetMessage() => "Hello World";
}

class GreetingService([Tag("base")] IService baseService) : IService
{
    public string GetMessage() => $"{baseService.GetMessage()} !!!";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Here an instance of the _Service_ type, labeled _"base"_, is injected in the decorator _DecoratorService_. You can use any tag that semantically reflects the feature of the abstraction being embedded. The tag can be a constant, a type, or a value of an enumerated type.

## Interception

Interception allows you to enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.

```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Runtime.CompilerServices;
using Pure.DI;

// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameWildcard = *IService
DI.Setup(nameof(Composition))
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.GetMessage().ShouldBe("Hello World !!!");

public interface IService
{
    string GetMessage();
}

class Service : IService
{
    public string GetMessage() => "Hello World";
}

partial class Composition : IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T).IsValueType)
        {
            return value;
        }

        return (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
            typeof(T),
            value,
            this);
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
        if (invocation.Method.Name == nameof(IService.GetMessage)
            && invocation.ReturnValue is string message)
        {
            invocation.ReturnValue = $"{message} !!!";
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Castle.DynamicProxy](https://www.nuget.org/packages/Castle.DynamicProxy)

Using an intercept gives you the ability to add end-to-end functionality such as:

- Logging

- Action logging

- Performance monitoring

- Security

- Caching

- Error handling

- Providing resistance to failures, etc.

## Advanced interception

This approach of interception maximizes performance by precompiling the proxy object factory.

```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Pure.DI;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var log = new List<string>();
var composition = new Composition(log);
var service = composition.Root;
service.ServiceRun();
service.Dependency.DependencyRun();

log.ShouldBe(
    ImmutableArray.Create(
        "ServiceRun returns Abc",
        "get_Dependency returns Castle.Proxies.IDependencyProxy",
        "DependencyRun returns 33"));

public interface IDependency
{
    int DependencyRun();
}

class Dependency : IDependency
{
    public int DependencyRun() => 33;
}

public interface IService
{
    IDependency Dependency { get; }

    string ServiceRun();
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public string ServiceRun() => "Abc";
}

internal partial class Composition : IInterceptor
{
    private readonly List<string> _log = [];
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors = [];

    public Composition(List<string> log)
        : this()
    {
        _log = log;
        _interceptors = [this];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T).IsValueType)
        {
            return value;
        }

        return ProxyFactory<T>.GetFactory(ProxyBuilder)(
            value,
            _interceptors);
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
        _log.Add($"{invocation.Method.Name} returns {invocation.ReturnValue}");
    }

    private static class ProxyFactory<T>
    {
        private static Func<T, IInterceptor[], T>? _factory;

        public static Func<T, IInterceptor[], T> GetFactory(IProxyBuilder proxyBuilder) =>
            _factory ?? CreateFactory(proxyBuilder);

        private static Func<T, IInterceptor[], T> CreateFactory(IProxyBuilder proxyBuilder)
        {
            // Compiles a delegate to create a proxy for the performance boost
            var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(
                typeof(T),
                Type.EmptyTypes,
                ProxyGenerationOptions.Default);
            var ctor = proxyType.GetConstructors()
                .Single(i => i.GetParameters().Length == 2);
            var instance = Expression.Parameter(typeof(T));
            var interceptors = Expression.Parameter(typeof(IInterceptor[]));
            var newProxyExpression = Expression.New(ctor, interceptors, instance);
            return _factory = Expression.Lambda<Func<T, IInterceptor[], T>>(
                    newProxyExpression,
                    instance,
                    interceptors)
                .Compile();
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Castle.DynamicProxy](https://www.nuget.org/packages/Castle.DynamicProxy)


## Resolve hint

Hints are used to fine-tune code generation. The _Resolve_ hint determines whether to generate _Resolve_ methods. By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time, and no anonymous composition roots will be generated in this case. When the _Resolve_ hint is disabled, only the regular root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// Resolve = Off`.

```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(Resolve, "Off")
    .Bind().To<Dependency>()
    .Root<IDependency>("DependencyRoot")
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencyRoot = composition.DependencyRoot;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

For more hints, see [this](README.md#setup-hints) page.

## ThreadSafe hint

Hints are used to fine-tune code generation. The _ThreadSafe_ hint determines whether object composition will be created in a thread-safe manner. This hint is _On_ by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ThreadSafe = Off`.

```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(ThreadSafe, "Off")
    .Bind().To<Dependency>()
    .Bind().As(Lifetime.Singleton).To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(Func<IDependency> dependencyFactory) : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

For more hints, see [this](README.md#setup-hints) page.

## OnDependencyInjection regular expression hint

Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Hint(OnDependencyInjectionContractTypeNameRegularExpression, "(.*IDependency|int)$")
    .RootArg<int>("id")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("GetRoot");

var log = new List<string>();
var composition = new Composition(log);
var service = composition.GetRoot(33);

log.ShouldBe([
    "Int32 injected",
    "Dependency injected"
]);

interface IDependency;

record Dependency(int Id) : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        _log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnDependencyInjectionContractTypeNameRegularExpression` hint helps identify the set of types that require injection control. You can use it to specify a regular expression to filter the full name of a type.
For more hints, see [this](README.md#setup-hints) page.

## OnDependencyInjection wildcard hint

Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IDependency")
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IService")
    .RootArg<int>("id")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("GetRoot");

var log = new List<string>();
var composition = new Composition(log);
var service = composition.GetRoot(33);

log.ShouldBe([
    "Dependency injected",
    "Service injected"]);

interface IDependency;

record Dependency(int Id) : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        _log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnDependencyInjectionContractTypeNameWildcard` hint helps identify the set of types that require injection control. You can use it to specify a wildcard to filter the full name of a type.
For more hints, see [this](README.md#setup-hints) page.

## OnCannotResolve regular expression hint

Hints are used to fine-tune code generation. The _OnCannotResolve_ hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnCannotResolveContractTypeNameRegularExpression = string`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnCannotResolveContractTypeNameRegularExpression = string
DI.Setup(nameof(Composition))
    .Hint(OnCannotResolve, "On")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ToString().ShouldBe("My name");


interface IDependency;

class Dependency(string name) : IDependency
{
    public override string ToString() => name;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)"My name";
        }

        throw new InvalidOperationException("Cannot resolve.");
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnCannotResolveContractTypeNameRegularExpression` hint helps define the set of types that require manual dependency resolution. You can use it to specify a regular expression to filter the full type name.
For more hints, see [this](README.md#setup-hints) page.

## OnCannotResolve wildcard hint

Hints are used to fine-tune code generation. The _OnCannotResolve_ hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnCannotResolveContractTypeNameWildcard = string`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnCannotResolveContractTypeNameWildcard = string
DI.Setup(nameof(Composition))
    .Hint(OnCannotResolve, "On")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ToString().ShouldBe("My name");


interface IDependency;

class Dependency(string name) : IDependency
{
    public override string ToString() => name;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)"My name";
        }

        throw new InvalidOperationException("Cannot resolve.");
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnCannotResolveContractTypeNameWildcard` hint helps define the set of types that require manual dependency resolution. You can use it to specify a wildcard to filter the full type name.
For more hints, see [this](README.md#setup-hints) page.

## OnNewInstance regular expression hint

Hints are used to fine-tune code generation. The _OnNewInstance_ hint determines whether to generate partial _OnNewInstance_ method.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnNewInstance = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(OnNewInstance, "On")
    .Hint(OnNewInstanceLifetimeRegularExpression, "(Singleton|PerBlock)")
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .Bind().As(Lifetime.PerBlock).To<Service>()
    .Root<IService>("Root");

var log = new List<string>();
var composition = new Composition(log);
var service1 = composition.Root;
var service2 = composition.Root;

log.ShouldBe([
    "Dependency created",
    "Service created",
    "Service created"]);

interface IDependency;

class Dependency : IDependency
{
    public override string ToString() => nameof(Dependency);
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public override string ToString() => nameof(Service);
}

internal partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        _log.Add($"{typeof(T).Name} created");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnNewInstanceLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
For more hints, see [this](README.md#setup-hints) page.

## OnNewInstance wildcard hint

Hints are used to fine-tune code generation. The _OnNewInstance_ hint determines whether to generate partial _OnNewInstance_ method.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnNewInstance = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(OnNewInstance, "On")
    .Hint(OnNewInstanceImplementationTypeNameWildcard, "*Dependency")
    .Hint(OnNewInstanceImplementationTypeNameWildcard, "*Service")
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var log = new List<string>();
var composition = new Composition(log);
var service1 = composition.Root;
var service2 = composition.Root;

log.ShouldBe([
    "Dependency created",
    "Service created",
    "Service created"]);

interface IDependency;

class Dependency : IDependency
{
    public override string ToString() => nameof(Dependency);
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public override string ToString() => nameof(Service);
}

internal partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        _log.Add($"{typeof(T).Name} created");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnNewInstanceImplementationTypeNameWildcard` hint helps you define a set of implementation types that require instance creation control. You can use it to specify a wildcard to filter bindings by implementation name.
For more hints, see [this](README.md#setup-hints) page.

## ToString hint

Hints are used to fine-tune code generation. The _ToString_ hint determines if the _ToString()_ method should be generated. This method provides a text-based class diagram in the format [mermaid](https://mermaid.js.org/). To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/). An example class diagram can be seen below.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ToString = On`.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Hint(Hint.ToString, "On")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("MyService");

var composition = new Composition();
string classDiagram = composition.ToString();

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Developers who start using DI technology often complain that they stop seeing the structure of the application because it is difficult to understand how it is built. To make life easier, you can add the _ToString_ hint by telling the generator to create a `ToString()` method.
For more hints, see [this](README.md#setup-hints) page.

## Check for a root

Sometimes you need to check if you can get the root of a composition using the _Resolve_ method before calling it, this example will show you how to do it:

```c#
using Shouldly;
using Pure.DI;

Composition.HasRoot(typeof(IService)).ShouldBeTrue();
Composition.HasRoot(typeof(IDependency), "MyDepTag").ShouldBeTrue();

Composition.HasRoot(typeof(IDependency)).ShouldBeFalse();
Composition.HasRoot(typeof(IComparable)).ShouldBeFalse();


interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    [Tag("MyDepTag")]
    public required IDependency Dependency { get; init; }
}

partial class Composition
{
    private static readonly HashSet<(Type type, object? tag)> Roots = [];

    // Check that the root can be resolved by Resolve methods
    internal static bool HasRoot(Type type, object? key = null) =>
        Roots.Contains((type, key));

    static void Setup() =>
        DI.Setup()
            // Specifies to use the partial OnNewRoot method
            // to register each root
            .Hint(Hint.OnNewRoot, "On")
            .Bind("MyDepTag").To<Dependency>()
            .Bind().To<Service>()

            // Composition roots
            .Root<IDependency>(tag: "MyDepTag")
            .Root<IService>("Root");

    // Adds a new root to the hash set
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) =>
        Roots.Add((typeof(TContract), tag));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

For more hints, see [this](README.md#setup-hints) page.

## Composition root kinds

```c#
using Pure.DI;
using static Pure.DI.RootKinds;

var composition = new Composition();
var service = composition.Root;
var otherService = composition.GetOtherService();
var dependency = Composition.Dependency;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

class OtherService : IService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IDependency>().To<Dependency>()

            // Creates a public root method named "GetOtherService"
            .Root<IService>("GetOtherService", "Other", Public | Method)

            // Creates a private partial root method named "GetRoot"
            .Root<IService>("GetRoot", kind: Private | Partial | Method)

            // Creates a internal static root named "Dependency"
            .Root<IDependency>("Dependency", kind: Internal | Static);

    private partial IService GetRoot();

    public IService Root => GetRoot();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Root with name template

```c#
using Pure.DI;

DI.Setup("Composition")
    .Root<Service>("My{type}");

var composition = new Composition();
var service = composition.MyService;

class Dependency;

class Service(Dependency dependency);
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Tag Any

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>(Tag.Any).To(ctx => new Dependency(ctx.Tag))
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root")

    // Root by Tag.Any
    .Root<IDependency>("OtherDependency", "Other");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.Key.ShouldBe("Abc");
service.Dependency2.Key.ShouldBe(123);
service.Dependency3.Key.ShouldBeNull();
composition.OtherDependency.Key.ShouldBe("Other");

interface IDependency
{
    object? Key { get; }
}

record Dependency(object? Key) : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag("Abc")] IDependency dependencyAbc,
    [Tag(123)] Func<IDependency> dependency123Factory,
    IDependency dependency)
    : IService
{
    public IDependency Dependency1 { get; } = dependencyAbc;

    public IDependency Dependency2 { get; } = dependency123Factory();

    public IDependency Dependency3 { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tag Type

`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Tag.Type here is the same as typeof(AbcDependency)
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IDependency>(Tag.Type, default).To<AbcDependency>()
    // Tag.Type here is the same as typeof(XyzDependency)
    .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root")

    // "XyzRoot" is root name, typeof(XyzDependency) is tag
    .Root<IDependency>("XyzRoot", typeof(XyzDependency));

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tag Unique

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var stringService = composition.Root;
stringService.Dependencies.Length.ShouldBe(2);

interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = [..dependencies];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tag on injection site

Sometimes it is necessary to determine which binding will be used to inject explicitly. To do this, use a special tag created by calling the `Tag.On()` method. Tag on injection site is specified in a special format: `Tag.On("<namespace>.<type>.<member>[:argument]")`. The argument is specified only for the constructor and methods. For example, for namespace _MyNamespace_ and type _Class1_:

- `Tag.On("MyNamespace.Class1.Class1:state1") - the tag corresponds to the constructor argument named _state_ of type _MyNamespace.Class1_
- `Tag.On("MyNamespace.Class1.DoSomething:myArg")` - the tag corresponds to the _myArg_ argument of the _DoSomething_ method
- `Tag.On("MyNamespace.Class1.MyData")` - the tag corresponds to property or field _MyData_

The wildcards `*` and `?` are supported. All names are case-sensitive. The global namespace prefix `global::` must be omitted. You can also combine multiple tags in a single `Tag.On("...", "...")` call.

For generic types, the type name also contains the number of type parameters, e.g., for the `myDep` constructor argument of the `Consumer<T>` class, the tag on the injection site would be ``MyNamespace.Consumer`1.Consumer:myDep``:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(
        Tag.On("*Service.Service:dependency1"),
        // Tag on injection site for generic type
        Tag.On("*Consumer`1.Consumer:myDep"))
        .To<AbcDependency>()
    .Bind(
        // Combined tag
        Tag.On(
            "*Service.Service:dependency2",
            "*Service:Dependency3"))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency3.ShouldBeOfType<XyzDependency>();
service.Dependency4.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }

    IDependency Dependency4 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public required IDependency Dependency3 { init; get; }

    public IDependency Dependency4 => consumer.Dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a constructor argument

The wildcards ‘*’ and ‘?’ are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.OnConstructorArg<Service>("dependency1"))
        .To<AbcDependency>()
    .Bind(Tag.OnConstructorArg<Consumer<TT>>("myDep"))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 => consumer.Dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a member

The wildcards ‘*’ and ‘?’ are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<AbcDependency>()
    .Bind(Tag.OnMember<Service>(nameof(Service.Dependency)))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public required IDependency Dependency { init; get; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a method argument

The wildcards ‘*’ and ‘?’ are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<AbcDependency>()
    .Bind(Tag.OnMethodArg<Service>(nameof(Service.Initialize), "dep"))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency? Dependency { get; }
}

class Service : IService
{
    [Dependency]
    public void Initialize(IDependency dep) =>
        Dependency = dep;

    public IDependency? Dependency { get; private set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on injection site with wildcards

The wildcards ‘*’ and ‘?’ are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.On("*Service:Dependency3", "*Consumer:myDep"))
        .To<AbcDependency>()
    .Bind(Tag.On("*Service:dependency?"))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency3.ShouldBeOfType<AbcDependency>();
service.Dependency4.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }

    IDependency Dependency4 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public required IDependency Dependency3 { init; get; }

    public IDependency Dependency4 => consumer.Dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Dependent compositions

The _Setup_ method has an additional argument _kind_, which defines the type of composition:
- _CompositionKind.Public_ - will create a normal composition class, this is the default setting and can be omitted, it can also use the _DependsOn_ method to use it as a dependency in other compositions
- _CompositionKind.Internal_ - the composition class will not be created, but that composition can be used to create other compositions by calling the _DependsOn_ method with its name
- _CompositionKind.Global_ - the composition class will also not be created, but that composition will automatically be used to create other compositions

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

// This setup does not generate code, but can be used as a dependency
// and requires the use of the "DependsOn" call to add it as a dependency
DI.Setup("BaseComposition", Internal)
    .Bind<IDependency>().To<Dependency>();

// This setup generates code and can also be used as a dependency
DI.Setup(nameof(Composition))
    // Uses "BaseComposition" setup
    .DependsOn("BaseComposition")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

// As in the previous case, this setup generates code and can also be used as a dependency
DI.Setup(nameof(OtherComposition))
    // Uses "Composition" setup
    .DependsOn(nameof(Composition))
    .Root<Program>("Program");

var composition = new Composition();
var service = composition.Root;

var otherComposition = new OtherComposition();
service = otherComposition.Program.Service;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Program(IService service)
{
    public IService Service { get; } = service;
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Inheritance of compositions

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

var composition = new Composition();
var service = composition.Root;

class BaseComposition
{
    private static void Setup() =>
        DI.Setup(kind: Internal)
            .Bind<IDependency>().To<Dependency>();
}

partial class Composition: BaseComposition
{
    private void Setup() =>
        DI.Setup()
            .Bind<IService>().To<Service>()
            .Root<Program>(nameof(Root));
}

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Program(IService service)
{
    public IService Service { get; } = service;
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Accumulators

Accumulators allow you to accumulate instances of certain types and lifetimes.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Accumulate<IAccumulating, MyAccumulator>(Transient, Singleton)
    .Bind<IDependency>().As(PerBlock).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).As(Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()
    .Root<(IService service, MyAccumulator accumulator)>("Root");

var composition = new Composition();
var (service, accumulator) = composition.Root;
accumulator.Count.ShouldBe(3);
accumulator[0].ShouldBeOfType<XyzDependency>();
accumulator[1].ShouldBeOfType<AbcDependency>();
accumulator[2].ShouldBeOfType<Service>();

interface IAccumulating;

class MyAccumulator : List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;

class XyzDependency : IDependency, IAccumulating;

interface IService;

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService, IAccumulating;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Global compositions

When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

return;

class MyGlobalComposition
{
    static void Setup() =>
        DI.Setup(kind: Global)
            .Hint(Hint.ToString, "Off")
            .Hint(Hint.FormatCode, "On");
}

class MyGlobalComposition2
{
    static void Setup() =>
        DI.Setup("Some name", kind: Global)
            .Hint(Hint.ToString, "On");
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Partial class

A partial class can contain setup code.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.RootKinds;
using System.Diagnostics;

var composition = new Composition("Abc");
var service = composition.Root;

service.Name.ShouldBe("Abc_3");
service.Dependency1.Id.ShouldBe(1);
service.Dependency2.Id.ShouldBe(2);

interface IDependency
{
    long Id { get; }
}

class Dependency(long id) : IDependency
{
    public long Id { get; } = id;
}

class Service(
    [Tag("name with id")] string name,
    IDependency dependency1,
    IDependency dependency2)
{
    public string Name { get; } = name;

    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}

// The partial class is also useful for specifying access modifiers to the generated class
public partial class Composition
{
    private readonly string _serviceName = "";
    private long _id;

    // Customizable constructor
    public Composition(string serviceName)
        : this()
    {
        _serviceName = serviceName;
    }

    private long GenerateId() => Interlocked.Increment(ref _id);

    // In fact, this method will not be called at runtime
    [Conditional("DI")]
    void Setup() =>

        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind<long>().To(_ => GenerateId())
            .Bind<string>("name with id").To(
                _ => $"{_serviceName}_{GenerateId()}")
            .Root<Service>("Root", kind: Internal);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The partial class is also useful for specifying access modifiers to the generated class.

## A few partial classes

The setting code for one Composition can be located in several methods and/or in several partial classes.

```c#
using Pure.DI;

var composition = new Composition();
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

partial class Composition
{

    // This method will not be called in runtime
    static void Setup1() =>
        DI.Setup()
            .Bind<IDependency>().To<Dependency>();
}

partial class Composition
{
    // This method will not be called in runtime
    static void Setup2() =>
        DI.Setup()
            .Bind<IService>().To<Service>();
}

partial class Composition
{
    // This method will not be called in runtime
    private static void Setup3() =>
        DI.Setup()
            .Root<IService>("Root");
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Thread-safe overrides

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;
using System.Drawing;

DI.Setup(nameof(Composition))
    .Bind(Tag.Red).To(_ => Color.Red)
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Func<int, int, IDependency>>(ctx =>
        (dependencyId, subId) =>
        {
            ctx.Inject(Tag.Red, out Color red);

            // Get composition sync root object
            ctx.Inject(Tag.SyncRoot, out Lock lockObject);
            lock (lockObject)
            {
                // Overrides with a lambda argument
                ctx.Override(dependencyId);

                // Overrides with tag using lambda argument
                ctx.Override(subId, "sub");

                // Overrides with some value
                ctx.Override($"Dep {dependencyId} {subId}");

                // Overrides with injected value
                ctx.Override(red);

                ctx.Inject<Dependency>(out var dependency);
                return dependency;
            }
        })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(100);
for (var i = 0; i < 100; i++)
{
    service.Dependencies.Count(dep => dep.Id == i).ShouldBe(1);
}

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    string Name { get; }

    int Id { get; }

    int SubId { get; }
}

class Dependency(
    string name,
    IClock clock,
    int id,
    [Tag("sub")] int subId,
    Color red)
    : IDependency
{
    public string Name => name;

    public int Id => id;

    public int SubId => subId;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<int, int, IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
        [
            ..Enumerable.Range(0, 100).AsParallel().Select(i => dependencyFactory(i, 99))
        ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Consumer types

`ConsumerTypes` is used to get the list of consumer types of a given dependency. It contains an array of types and guarantees that it will contain at least one element. The use of `ConsumerTypes` is demonstrated on the example of [Serilog library](https://serilog.net/):

```c#
using Shouldly;
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using Serilog.Core;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency
{
    public Dependency(Serilog.ILogger log)
    {
        log.Information("created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        Serilog.ILogger log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx =>
            {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);
                return logger.ForContext(ctx.ConsumerTypes[0]);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)


## Tracking disposable instances per a composition root

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Value.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IService>>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tracking disposable instances in delegates

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(Func<Owned<IDependency>> dependencyFactory)
    : IService, IDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public void Dispose() => _dependency.Dispose();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tracking disposable instances using pre-built classes

If you want ready-made classes for tracking disposable objects in your libraries but don't want to create your own, you can add this package to your projects:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.

```c#
using Shouldly;
using Pure.DI.Abstractions;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
root1.Dependency.ShouldNotBe(root2.Dependency);
root1.SingleDependency.ShouldBe(root2.SingleDependency);

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root2.SingleDependency.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();
root1.SingleDependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root1.SingleDependency.IsDisposed.ShouldBeFalse();
        
composition.Dispose();
root1.SingleDependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }

    public IDependency SingleDependency { get; }
}

class Service(
    Func<Own<IDependency>> dependencyFactory,
    [Tag("single")] Func<Own<IDependency>> singleDependencyFactory)
    : IService, IDisposable
{
    private readonly Own<IDependency> _dependency = dependencyFactory();
    private readonly Own<IDependency> _singleDependency = singleDependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public IDependency SingleDependency => _singleDependency.Value;

    public void Dispose()
    {
        _dependency.Dispose();
        _singleDependency.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind("single").As(Lifetime.Singleton).To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Tracking disposable instances with different lifetimes

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
root1.Dependency.ShouldNotBe(root2.Dependency);
root1.SingleDependency.ShouldBe(root2.SingleDependency);

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root2.SingleDependency.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();
root1.SingleDependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root1.SingleDependency.IsDisposed.ShouldBeFalse();
        
composition.Dispose();
root1.SingleDependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }

    public IDependency SingleDependency { get; }
}

class Service(
    Func<Owned<IDependency>> dependencyFactory,
    [Tag("single")] Func<Owned<IDependency>> singleDependencyFactory)
    : IService, IDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();
    private readonly Owned<IDependency> _singleDependency = singleDependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public IDependency SingleDependency => _singleDependency.Value;

    public void Dispose()
    {
        _dependency.Dispose();
        _singleDependency.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind("single").As(Lifetime.Singleton).To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tracking async disposable instances per a composition root

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

await root2.DisposeAsync();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Value.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeFalse();

await root1.DisposeAsync();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Value.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IService>>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Tracking async disposable instances in delegates

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;

await root2.DisposeAsync();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();

await root1.DisposeAsync();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(Func<Owned<IDependency>> dependencyFactory)
    : IService, IAsyncDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public ValueTask DisposeAsync()
    {
        return _dependency.DisposeAsync();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Exposed roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exposed roots with tags

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithTagsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind("Some tag").To<MyService>()
            .Root<IMyService>("MyService", "Some tag", RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithTagsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program([Tag("Some tag")] IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Exposed roots via arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through class arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Arg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("Program");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition(baseComposition);
var program = composition.Program;
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Exposed roots via root arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .RootArg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("GetProgram");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition();
var program = composition.GetProgram(baseComposition);
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Exposed generic roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
    DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind().To(_ => 99)
        .Bind().As(Lifetime.Singleton).To<MyDependency>()
        .Bind().To<MyGenericService<TT>>()
        .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething(99);

partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

> [!IMPORTANT]
> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exposed generic roots with args

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithGenericRootsAndArgsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .RootArg<int>("id")
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyGenericService<TT>>()
            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    .RootArg<int>("id")
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
    .Root<Program>("GetProgram");

var composition = new Composition();
var program = composition.GetProgram(33);
program.DoSomething(99);

partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## AutoMapper

```c#
using Shouldly;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI.Abstractions;
using Pure.DI;
using Pure.DI.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;

var logMessages = new List<string>();
using var composition = new Composition(logMessages);
var root = composition.Root;

root.Run();
logMessages.ShouldContain("John Smith");

class Person
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    [Inject]
    public IPersonFormatter? Formatter { get; set; }
}

class Student
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime? AdmissionDate { get; set; }
}

interface IPersonFormatter
{
    string Format(Person person);
}

class PersonFormatter : IPersonFormatter
{
    public string Format(Person person) => $"{person.FirstName} {person.LastName}";
}

interface IStudentService
{
    string AsPersonText(Student student);
}

class StudentService(Func<Student, Person> map) : IStudentService
{
    public string AsPersonText(Student student)
    {
        var person = map(student);
        return person.Formatter?.Format(person) ?? "";
    }
}

partial class Program(ILogger logger, IStudentService studentService)
{
    public void Run()
    {
        var nik = new Student { FirstName = "John", LastName = "Smith" };
        var personText = studentService.AsPersonText(nik);
        logger.LogInformation(personText);
    }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Root<Program>(nameof(Root))
            .Arg<ICollection<string>>("logMessage")
            // Example dependency for Program
            .Bind().To<StudentService>()

            .DefaultLifetime(Singleton)
                // Example dependency for Person
                .Bind().To<PersonFormatter>()
                // Logger for AutoMapper
                .Bind().To<LoggerFactory>()
                .Bind().To((LoggerFactory loggerFactory) => loggerFactory.CreateLogger("info"))
                // Provides a mapper
                .Bind<IMapper>().To<LoggerFactory, Mapper>(loggerFactory => {
                    // Create the mapping configuration
                    var configuration = new MapperConfiguration(cfg => {
                            cfg.CreateMap<Student, Person>();
                        },
                        loggerFactory);
                    configuration.CompileMappings();
                    // Create the mapper
                    return new Mapper(configuration);
                })
                // Maps TT1 -> TT2
                .Bind().To<Func<TT1, TT2>>(ctx => source => {
                    ctx.Inject(out IMapper mapper);
                    // source -> target
                    var target = mapper.Map<TT1, TT2>(source);
                    // Building-up a mapped value with dependencies
                    ctx.BuildUp(target);
                    return target;
                });
}

class LoggerFactory(ICollection<string> logMessages)
    : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) {}

    public ILogger CreateLogger(string categoryName) => new Logger(logMessages);

    public void Dispose() { }

    private class Logger(ICollection<string> logMessages): ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logMessages.Add(formatter(state, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [AutoMapper](https://www.nuget.org/packages/AutoMapper)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)


## JSON serialization

```c#
using Shouldly;
using Pure.DI;
using System.Text.Json;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;

var composition = new Composition();
var settings = composition.Settings;
settings.Size.ShouldBe(10);

settings.Size = 99;
settings.Size.ShouldBe(99);

settings.Size = 33;
settings.Size.ShouldBe(33);

record Settings(int Size)
{
    public static readonly Settings Default = new(10);
}

interface IStorage
{
    void Save(string data);

    string? Load();
}

class Storage : IStorage
{
    private string? _data;

    public void Save(string data) => _data = data;

    public string? Load() => _data;
}

interface ISettingsService
{
    int Size { get; set; }
}

class SettingsService(
    [Tag(JSON)] Func<string, Settings?> deserialize,
    [Tag(JSON)] Func<Settings, string> serialize,
    IStorage storage)
    : ISettingsService
{
    public int Size
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        get => GetSettings().Size;
        set => SaveSettings(GetSettings() with { Size = value });
    }

    private Settings GetSettings() =>
        storage.Load() is {} data && deserialize(data) is {} settings
            ? settings
            : Settings.Default;

    private void SaveSettings(Settings settings) =>
        storage.Save(serialize(settings));
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Root<ISettingsService>(nameof(Settings))
            .Bind().To<SettingsService>()
            .DefaultLifetime(Singleton)
            .Bind().To(_ => new JsonSerializerOptions { WriteIndented = true })
            .Bind(JSON).To<JsonSerializerOptions, Func<string, TT?>>(options => json => JsonSerializer.Deserialize<TT>(json, options))
            .Bind(JSON).To<JsonSerializerOptions, Func<TT, string>>(options => value => JsonSerializer.Serialize(value, options))
            .Bind().To<Storage>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Serilog

```c#
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using System.Runtime.CompilerServices;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(Serilog.ILogger log, IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Hint(Hint.OnNewInstance, "On")
            .Hint(Hint.OnDependencyInjection, "On")
            // Excluding loggers
            .Hint(Hint.OnNewInstanceImplementationTypeNameRegularExpression, "^((?!Logger).)*$")
            .Hint(Hint.OnDependencyInjectionContractTypeNameRegularExpression, "^((?!Logger).)*$")

            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind<Serilog.ILogger>().To(ctx =>
            {
                ctx.Inject("from arg", out Serilog.ILogger logger);
                return logger.ForContext(ctx.ConsumerType);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Serilog.ILogger>(nameof(Log), kind: RootKinds.Private)
            .Root<IService>(nameof(Root));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime) =>
        Log.Information("Created [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        Log.Information("Injected [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)


## Basic Unity use case

```c#
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

    [SerializeField]
    private Transform hoursPivot;

    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;

        hoursPivot.localRotation = Quaternion
            .Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);

        minutesPivot.localRotation = Quaternion
            .Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);

        secondsPivot.localRotation = Quaternion
            .Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService
{
    public DateTime Now => DateTime.Now;
}

internal partial class Composition
{
    public static readonly Composition Shared = new();

    private static void Setup() =>

        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<ClockService>()
            .Builder<Clock>();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


## Unity MonoBehaviours

```c#
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

    [SerializeField]
    private Transform hoursPivot;

    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;

        hoursPivot.localRotation = Quaternion
            .Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);

        minutesPivot.localRotation = Quaternion
            .Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);

        secondsPivot.localRotation = Quaternion
            .Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public class OtherClock : MonoBehaviour
{
    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    public void Update()
    {
        // ReSharper disable once UnusedVariable
        var now = ClockService.Now.TimeOfDay;
    }
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService
{
    public DateTime Now => DateTime.Now;
}

internal partial class Composition
{
    public static readonly Composition Shared = new();

    private static void Setup() =>

        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<ClockService>()
            // Creates a builder for each type inherited from MonoBehaviour.
            // These types must be available at this point in the code.
            .Builders<MonoBehaviour>();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)


# Examples of using Pure.DI for different types of .NET projects.

#### Avalonia application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/AvaloniaApp)

This example demonstrates the creation of a [Avalonia](https://avaloniaui.net/) application in the pure DI paradigm using the Pure.DI code generator.

> [!NOTE]
> [Another example](samples/SingleRootAvaloniaApp) with Avalonia shows how to create an application with a single composition root.

The definition of the composition is in [Composition.cs](/samples/AvaloniaApp/Composition.cs). This class setups how the composition of objects will be created for the application. You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .OrdinalAttribute<InitializableAttribute>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/AvaloniaApp/App.axaml) for later use within the _xaml_ markup everywhere:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="AvaloniaApp.App"
             xmlns:app="using:AvaloniaApp"
             RequestedThemeVariant="Default">

  <!-- "Default" ThemeVariant follows system theme variant.
  "Dark" or "Light" are other available options. -->
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>

  <Application.Resources>
    <!--Creates a shared resource of type Composition and with key "Composition",
    which will be further used as a data context in the views.-->
    <app:Composition x:Key="Composition"  />
  </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` with key _"Composition"_, which will be further used as a data context in the views.

The associated application [App.axaml.cs](/samples/AvaloniaApp/App.axaml.cs) class is looking like:

```c#
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Resources[nameof(Composition)] is Composition composition)
        {
            // Assigns the main window/view
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow();
                    break;

                case ISingleViewApplicationLifetime singleView:
                    singleView.MainView = new MainWindow();
                    break;
            }

            // Handles disposables
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Exit += (_, _) => composition.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

Advantages over classical DI container libraries:
- No explicit initialisation of data contexts is required. Data contexts are configured directly in `.axaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings and use the code-behind file-less approach. All previously defined composition roots are now available from [markup](/samples/AvaloniaApp/Views/MainWindow.xaml) without any effort, e.g. _Clock_:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:AvaloniaApp"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        x:Class="AvaloniaApp.MainWindow"
        DataContext="{StaticResource Composition}"
        x:DataType="app:Composition"
        Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}"
        Icon="/Assets/avalonia-logo.ico"
        FontFamily="Consolas"
        FontWeight="Bold">

  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>

</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Specify the data type in the context:

  `xmlns:app="clr-namespace:AvaloniaApp"`

  `x:DataType="app:Composition"`

- Use the bindings as usual:

```xml
  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>
```

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/AvaloniaApp/AvaloniaApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Blazor Server application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

This example demonstrates the creation of a [Blazor server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorServerApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/BlazorServerApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/BlazorServerApp/BlazorServerApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### Blazor WebAssembly application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

[Here's an example](https://devteam.github.io/Pure.DI/) on github.io

This example demonstrates the creation of a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/BlazorWebAssemblyApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorWebAssemblyApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/BlazorWebAssemblyApp/Program.cs) file:

```c#
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.ConfigureContainer(composition);
```

The [project file](/samples/BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### Console native AOT application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatNativeAOT)

This example is very similar to [simple console application](ConsoleTemplate.md), except that this is [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Schrödinger's cat console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCat)

This example demonstrates the creation of a simple console application in the pure DI paradigm using the Pure.DI code generator. All code is in [one file](/samples/ShroedingersCat/Program.cs) for easy perception:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample;

// Let's create an abstraction

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }
}

public enum State
{
    Alive,
    Dead
}

// Here is our implementation

public class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

// Let's glue all together

internal partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // Here the setup is part of the generated class, just as an example.
    void Setup() => DI.Setup()
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        // Represents schrodinger's cat
        .Bind().To<ShroedingersCat>()
        // Represents a cardboard box with any content
        .Bind().To<CardboardBox<TT>>()
        // Composition Root
        .Root<Program>("Root");
}

// Time to open boxes!

public class Program(IBox<ICat> box)
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs for an application take place
    public static void Main() => new Composition().Root.Run();

    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Top level statements console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatTopLevelStatements)

This example is very similar to [simple console application](ConsoleTemplate.md), except that the composition is [defined](/samples/ShroedingersCatTopLevelStatements/Program.cs) as top-level statements and looks a little less verbose:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
DI.Setup("Composition")
    // Models a random subatomic event that may or may not occur
    .Bind().As(Singleton).To<Random>()
    // Represents a quantum superposition of 2 states: Alive or Dead
    .Bind().To((Random random) => (State)random.Next(2))
    // Represents schrodinger's cat
    .Bind().To<ShroedingersCat>()
    // Represents a cardboard box with any content
    .Bind().To<CardboardBox<TT>>()
    // Composition Root
    .Root<Program>("Root");

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }
}

public enum State
{
    Alive,
    Dead
}

public class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

public partial class Program(IBox<ICat> box)
{
    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCatTopLevelStatements/ShroedingersCatTopLevelStatements.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### gRPC service

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/EF)

This example demonstrates the creation of an Entity Framework application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/EF/Composition.cs):

```c#
using System.Diagnostics;
using Pure.DI;
using Pure.DI.MS;

namespace EF;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Lifetime.PerResolve).To<PersonService>()
        .Bind().As(Lifetime.Singleton).To<ContactService>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The console application entry point is in the [Program.cs](/samples/EF/Program.cs) file:

```c#
using EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var composition = new Composition
{
    ServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .AddDbContext<PersonsDbContext>(options => options.UseInMemoryDatabase("Database of persons"))
        .BuildServiceProvider()
};

var root = composition.Root;
await root.RunAsync();

partial class Program(
    PersonsDbContext db,
    Func<Person> newPerson,
    Func<Contact> newContact)
{
    private async Task RunAsync()
    {
        var nik = newPerson() with
        {
            Name = "Nik",
            Contacts =
            [
                newContact() with { PhoneNumber = "+123456789" }
            ]
        };

        db.Persons.Add(nik);

        var john = newPerson() with
        {
            Name = "John",
            Contacts =
            [
                newContact() with { PhoneNumber = "+777333444" },
                newContact() with { PhoneNumber = "+999888666" }
            ]
        };

        db.Persons.Add(john);

        await db.SaveChangesAsync();
        await db.Persons.ForEachAsync(Console.WriteLine);
    }
}
```

The [project file](/samples/EF/EF.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### gRPC service

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/GrpcService)

This example demonstrates the creation of a Web API application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/GrpcService/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace GrpcService;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<ClockService>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/GrpcService/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
...
```

The [project file](/samples/GrpcService/GrpcService.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### MAUI application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MAUIApp)

This example demonstrates the creation of a [MAUI application](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/MAUIApp/Composition.cs). You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<MauiDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [MauiProgram.cs](/samples/MAUIApp/MauiProgram.cs) file:

```c#
namespace MAUIApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var composition = new Composition();
        
        // Uses Composition as an alternative IServiceProviderFactory
        builder.ConfigureContainer(composition);
        
        builder
            .UseMauiApp(_ => new App
            {
                // Overrides the resource with an initialized Composition instance
                Resources = { ["Composition"] = composition }
            })
            .ConfigureLifecycleEvents(events =>
            {
                // Handles disposables
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnClosed((_, _) => composition.Dispose()));
#endif
#if ANDROID
                events.AddAndroid(android => android
                    .OnStop(_ => composition.Dispose()));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/MAUIApp/App.xaml) for later use within the _xaml_ markup everywhere:

```xaml
<?xml version="1.0" encoding="UTF-8"?>

<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:MAUIApp"
             x:Class="MAUIApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <local:Composition x:Key="Composition" />
                </ResourceDictionary>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

All previously defined composition roots are now accessible from [markup](/samples/MAUIApp/MainWindow.xaml) without any effort:

```xaml
<?xml version="1.0" encoding="utf-8"?>

<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MAUIApp.MainPage"
             xmlns:local="clr-namespace:MAUIApp"
             xmlns:clock="clr-namespace:Clock;assembly=Clock"
             BindingContext="{Binding Source={StaticResource Composition}, x:DataType=local:Composition, Path=Clock}"
             x:DataType="clock:IClockViewModel">

    <ScrollView>

        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center">

            <Label
                Text="{Binding Date}"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="64"
                HorizontalOptions="Center" />

            <Label
                Text="{Binding Time}"
                SemanticProperties.HeadingLevel="Level2"
                FontSize="128"
                HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

The [project file](/samples/MAUIApp/MAUIApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |


#### Minimal Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MinimalWebAPI)

This example demonstrates the creation of a Minimal Web API application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/MinimalWebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MinimalWebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        // Owned is used here to dispose of all disposable instances associated with the root.
        .Root<Owned<Program>>(nameof(Root))
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/MinimalWebAPI/Program.cs) file:

```c#
using MinimalWebAPI;

using var composition = new Composition();
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

var app = builder.Build();

// Creates an application composition root of type `Owned<Program>`
using var root = composition.Root;
root.Value.Run(app);

partial class Program(
    IClockViewModel clock,
    IAppViewModel appModel)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", (
            // Dependencies can be injected here as well
            [FromServices] ILogger<Program> logger) => {
            logger.LogInformation("Start of request execution");
            return new ClockResult(appModel.Title, clock.Date, clock.Time);
        });

        app.Run();
    }
}
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### Unity

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnityApp)

This example demonstrates the creation of a [Unity](https://unity.com/) application in the pure DI paradigm using the Pure.DI code generator.

![Unity](https://cdn.sanity.io/images/fuvbjjlp/production/01c082f3046cc45548249c31406aeffd0a9a738e-296x100.png)

The definition of the composition is in [Composition.cs](/samples/UnityApp/Assets/Scripts/Composition.cs). This class setups how the composition of objects will be created for the application. Don't forget to define builders for types inherited from `MonoBehaviour`:

```csharp
using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();

    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ClockService>()
        .Builders<MonoBehaviour>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

For types inherited from `MonoBehaviour`, a `BuildUp` composition method will be generated. This method will look as follows:

```csharp
[CompilerServices.MethodImpl(MethodImplOptions.AggressiveInlining)]
public Clock BuildUp(Clock buildingInstance)
{
    if (buildingInstance is null) 
        throw new ArgumentNullException(nameof(buildingInstance));

    if (_clockService is null)
        lock (_lock)
            if (_clockService is null)
                _clockService = new ClockService();

    buildingInstance.ClockService = _clockService;
    return buildingInstance;
}
```

A single instance of the _Composition_ class is defined as a public property `Composition.Shared`. It provides a common composition for building classes based on `MonoBehaviour`.

An [example](/samples/UnityApp/Assets/Scripts/Clock.cs) of such a class might look like this:

```c#
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

    [SerializeField]
    private Transform hoursPivot;
    
    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}
```

The Unity project should reference the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

The Unity example uses the Unity editor version 6000.0.35f1

#### Wep API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebAPI)

This example demonstrates the creation of a Web API application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/WebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebAPI;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/WebAPI/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### Wep application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebApp)

This example demonstrates the creation of a Web application in the pure DI paradigm using the Pure.DI code generator.

Composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself.

The web application entry point is in the [Program.cs](/samples/WebApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddControllersAsServices();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/WebApp/WebApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.11" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons on Pure.DI to work with Microsoft DI |

#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsAppNetCore)

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the Pure.DI code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary roots of the composition, for example, this could be a main form such as _FormMain_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsAppNetCore;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}
```

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsAppNetCore/Program.cs) file:

```c#
namespace WinFormsAppNetCore;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var composition = new Composition();
        composition.Root.Run();
    }

    private void Run() => Application.Run(formMain);
}
```

The [project file](/samples/WinFormsAppNetCore/WinFormsAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsApp)

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the Pure.DI code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsApp/Composition.cs). Remember to define all the necessary roots of the composition, for example, this could be a main form such as _FormMain_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsApp;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}
```

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsApp/Program.cs) file:

```c#
namespace WinFormsApp;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var composition = new Composition();
        var root = composition.Root;
        root.Run();
    }

    private void Run() => Application.Run(formMain);
}
```

The [project file](/samples/WinFormsApp/WinFormsApp.csproj) looks like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### WPF application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WpfAppNetCore)

This example demonstrates the creation of a WPF application in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). This class setups how the composition of objects will be created for the application. You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WpfAppNetCore;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WpfDispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/WpfAppNetCore/App.xaml) for later use within the _xaml_ markup everywhere:

```xaml
<Application x:Class="WpfAppNetCore.App" x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="clr-namespace:WpfAppNetCore"
             StartupUri="/MainWindow.xaml"
             Exit="OnExit">

    <Application.Resources>
        <!--Creates a shared resource of type Composition and with key "Composition",
        which will be further used as a data context in the views.-->
        <app:Composition x:Key="Composition" />
    </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` and with key _"Composition"_, which will be further used as a data context in the views.

Advantages over classical DI container libraries:
- No explicit initialisation of data contexts is required. Data contexts are configured directly in `.axaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings to model views without even editing the views `.cs` code files. All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:WpfAppNetCore"
        mc:Ignorable="d"
        DataContext="{StaticResource Composition}"
        d:DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center"/>
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center"/>        
    </StackPanel>
</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `d:DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Use the bindings as usual:

  `Title="{Binding App.Title}"`

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
   ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |


# Pure.DI API


<details><summary>Pure.DI</summary><blockquote>


<details><summary>BindAttribute</summary><blockquote>

Indicates that a property or method can be automatically added as a binding.
            
```c#

internal class DependencyProvider
            {
                [Bind()]
                public Dependency Dep => new Dependency();
            }
            
```


```c#

internal class DependencyProvider
            {
                [Bind(typeof(IDependency<TT>), Lifetime.Singleton)]
                public Dependency GetDep<T>() => new Dependency();
            }
            
```


```c#

internal class DependencyProvider
            {
                [Bind(typeof(IDependency), Lifetime.PerResolve, "some tag")]
                public Dependency GetDep(int id) => new Dependency(id);
            }
            
```


See also _Exposed_.

<details><summary>Constructor BindAttribute(System.Type,Pure.DI.Lifetime,System.Object[])</summary><blockquote>

Creates an attribute instance.
            
</blockquote></details>


</blockquote></details>


<details><summary>Buckets`1</summary><blockquote>

For internal use. 
            
</blockquote></details>


<details><summary>CompositionKind</summary><blockquote>

Determines how the partial class will be generated. The _Setup(System.String,Pure.DI.CompositionKind)_ method has an additional argument  `kind` , which defines the type of composition:
            
```c#

DI.Setup("BaseComposition", CompositionKind.Internal);
            
```


See also _Setup(System.String,Pure.DI.CompositionKind)_.

<details><summary>Field Public</summary><blockquote>

This value is used by default. If this value is specified, a normal partial class will be generated.
            
</blockquote></details>


<details><summary>Field Internal</summary><blockquote>

If this value is specified, the class will not be generated, but this setting can be used by other users as a baseline. The API call _DependsOn(System.String[])_ is mandatory.
            
</blockquote></details>


<details><summary>Field Global</summary><blockquote>

No partial classes will be created when this value is specified, but this setting is the baseline for all installations in the current project, and the API call _DependsOn(System.String[])_ is not required.
            
</blockquote></details>


</blockquote></details>


<details><summary>DependencyAttribute</summary><blockquote>

A universal DI attribute that allows to specify the tag and ordinal of an injection.
            
 - parameter _tag_ - The injection tag. See also _Tags(System.Object[])_
.
            
 - parameter _ordinal_ - The injection ordinal.

See also _OrdinalAttribute_.

See also _TagAttribute_.

<details><summary>Constructor DependencyAttribute(System.Object,System.Int32)</summary><blockquote>

Creates an attribute instance.
            
 - parameter _tag_ - The injection tag. See also _Tags(System.Object[])_
.
            
 - parameter _ordinal_ - The injection ordinal.

</blockquote></details>


</blockquote></details>


<details><summary>DI</summary><blockquote>

An API for a Dependency Injection setup.
            
See also _Setup(System.String,Pure.DI.CompositionKind)_.

<details><summary>Method Setup(System.String,Pure.DI.CompositionKind)</summary><blockquote>

Begins the definitions of the Dependency Injection setup chain.
             
```c#

interface IDependency;
            
             
             class Dependency: IDependency;
            
             
             interface IService;
            
             
             class Service(IDependency dependency): IService;
            
             
             DI.Setup("Composition")
               .Bind<IDependency>().To<Dependency>()
               .Bind<IService>().To<Service>()
               .Root<IService>("Root");
             
```


 - parameter _compositionTypeName_ - An optional argument specifying the partial class name to generate.

 - parameter _kind_ - An optional argument specifying the kind of setup. Please _CompositionKind_ for details. It defaults to  `Public` .

 - returns Reference to the setup continuation chain.

</blockquote></details>


</blockquote></details>


<details><summary>GenericTypeArgumentAttribute</summary><blockquote>

Represents a generic type argument attribute. It allows you to create custom generic type argument such as _TTS_, _TTDictionary`2_, etc.
            
```c#

[GenericTypeArgument]
            internal interface TTMy: IMy { }
            
```


See also _GenericTypeArgumentAttribute``1_.

See also _GenericTypeArgument``1_.

</blockquote></details>


<details><summary>Hint</summary><blockquote>

Hints for the code generator and can be used to fine-tune code generation.
            
```c#

// Resolve = Off
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.Resolve, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

<details><summary>Field Resolve</summary><blockquote>

 `On`  or  `Off` . Determines whether to generate  `Resolve`  methods.  `On`  by default.
            
```c#

// Resolve = Off
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.Resolve, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstance</summary><blockquote>

 `On`  or  `Off` . Determines whether to use partial  `OnNewInstance`  method.  `Off`  by default.
            
```c#

// OnNewInstance = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstance, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstancePartial</summary><blockquote>

 `On`  or  `Off` . Determines whether to generate partial  `OnNewInstance`  method when the _OnNewInstance_ hint is  `On` .  `On`  by default.
            
```c#

// OnNewInstancePartial = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstancePartial, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceImplementationTypeNameRegularExpression</summary><blockquote>

The regular expression to filter OnNewInstance by the instance type name. ".+" by default.
            
```c#

// OnNewInstanceImplementationTypeNameRegularExpression = Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceImplementationTypeNameRegularExpression, "Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceImplementationTypeNameWildcard</summary><blockquote>

The wildcard to filter OnNewInstance by the instance type name. "*" by default.
            
```c#

// OnNewInstanceImplementationTypeNameWildcard = *Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceImplementationTypeNameWildcard, "*Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceTagRegularExpression</summary><blockquote>

The regular expression to filter OnNewInstance by the tag. ".+" by default.
            
```c#

// OnNewInstanceTagRegularExpression = IDependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceTagRegularExpression, "IDependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceTagWildcard</summary><blockquote>

The wildcard to filter OnNewInstance by the tag. "*" by default.
            
```c#

// OnNewInstanceTagWildcard = *IDependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceTagWildcard, "*IDependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceLifetimeRegularExpression</summary><blockquote>

The regular expression to filter OnNewInstance by the lifetime. ".+" by default.
            
```c#

// OnNewInstanceLifetimeRegularExpression = Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceLifetimeRegularExpression, "Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewInstanceLifetimeWildcard</summary><blockquote>

The wildcard to filter OnNewInstance by the lifetime. "*" by default.
            
```c#

// OnNewInstanceLifetimeWildcard = *Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewInstanceLifetimeWildcard, "*Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjection</summary><blockquote>

 `On`  or  `Off` . Determines whether to use partial  `OnDependencyInjection`  method to control of a dependency injection.  `Off`  by default.
            
```c#

// OnDependencyInjection = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjection, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionPartial</summary><blockquote>

 `On`  or  `Off` . Determines whether to generate partial  `OnDependencyInjection`  method when the _OnDependencyInjection_ hint is  `On`  to control of dependency injection.  `On`  by default.
            
```c#

// OnDependencyInjectionPartial = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionPartial, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionImplementationTypeNameRegularExpression</summary><blockquote>

The regular expression to filter OnDependencyInjection by the instance type name. ".+" by default.
            
```c#

// OnDependencyInjectionImplementationTypeNameRegularExpression = Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, "Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionImplementationTypeNameWildcard</summary><blockquote>

The wildcard to filter OnDependencyInjection by the instance type name. "*" by default.
            
```c#

// OnDependencyInjectionImplementationTypeNameWildcard = *Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionImplementationTypeNameWildcard, "*Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionContractTypeNameRegularExpression</summary><blockquote>

The regular expression to filter OnDependencyInjection by the resolving type name. ".+" by default.
            
```c#

// OnDependencyInjectionContractTypeNameRegularExpression = IDependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionContractTypeNameRegularExpression, "IDependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionContractTypeNameWildcard</summary><blockquote>

The wildcard to filter OnDependencyInjection by the resolving type name. "*" by default.
            
```c#

// OnDependencyInjectionContractTypeNameWildcard = *IDependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionContractTypeName, "*IDependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionTagRegularExpression</summary><blockquote>

The regular expression to filter OnDependencyInjection by the tag. ".+" by default.
            
```c#

// OnDependencyInjectionTagRegularExpression = MyTag
            DI.Setup("Composition")
                .Bind<IDependency>("MyTag").To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionTagRegularExpression, "MyTag")
                .Bind<IDependency>("MyTag").To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionTagWildcard</summary><blockquote>

The wildcard to filter OnDependencyInjection by the tag. "*" by default.
            
```c#

// OnDependencyInjectionTagWildcard = MyTag
            DI.Setup("Composition")
                .Bind<IDependency>("MyTag").To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionTagWildcard, "MyTag")
                .Bind<IDependency>("MyTag").To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionLifetimeRegularExpression</summary><blockquote>

The regular expression to filter OnDependencyInjection by the lifetime. ".+" by default.
            
```c#

// OnDependencyInjectionLifetimeRegularExpression = Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionLifetimeRegularExpression, "Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnDependencyInjectionLifetimeWildcard</summary><blockquote>

The wildcard to filter OnDependencyInjection by the lifetime. ".+" by default.
            
```c#

// OnDependencyInjectionLifetimeWildcard = *Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnDependencyInjectionLifetimeWildcard, "*Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolve</summary><blockquote>

 `On`  or  `Off` . Determines whether to use a partial  `OnCannotResolve<T>(...)`  method to handle a scenario in which the dependency cannot be resolved.  `Off`  by default.
            
```c#

// OnCannotResolve = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolve, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolvePartial</summary><blockquote>

 `On`  or  `Off` . Determines whether to generate a partial  `OnCannotResolve<T>(...)`  method when the  `OnCannotResolve`  hint is  `On`  to handle a scenario in which the dependency cannot be resolved.  `On`  by default.
            
```c#

// OnCannotResolvePartial = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolvePartial, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveContractTypeNameRegularExpression</summary><blockquote>

The regular expression to filter OnCannotResolve by the resolving type name. ".+" by default.
            
```c#

// OnCannotResolveContractTypeNameRegularExpression = OtherType
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveContractTypeNameRegularExpression, "OtherType")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveContractTypeNameWildcard</summary><blockquote>

The wildcard to filter OnCannotResolve by the resolving type name. "*" by default.
            
```c#

// OnCannotResolveContractTypeNameWildcard = *OtherType
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveContractTypeNameWildcard, "*OtherType")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveTagRegularExpression</summary><blockquote>

The regular expression to filter OnCannotResolve by the tag. ".+" by default.
            
```c#

// OnCannotResolveTagRegularExpression = MyTag
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveTagRegularExpression, "MyTag")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveTagWildcard</summary><blockquote>

The wildcard to filter OnCannotResolve by the tag. "*" by default.
            
```c#

// OnCannotResolveTagWildcard = MyTag
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveTagWildcard, "MyTag")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveLifetimeRegularExpression</summary><blockquote>

The regular expression to filter OnCannotResolve by the lifetime. ".+" by default.
            
```c#

// OnCannotResolveLifetimeRegularExpression = Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveLifetimeRegularExpression, "Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnCannotResolveLifetimeWildcard</summary><blockquote>

The wildcard to filter OnCannotResolve by the lifetime. "*" by default.
            
```c#

// OnCannotResolveLifetimeWildcard = *Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnCannotResolveLifetimeWildcard, "*Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewRoot</summary><blockquote>

 `On`  or  `Off` . Determines whether to use a static partial  `OnNewRoot<T>(...)`  method to handle the new composition root registration event.  `Off`  by default.
            
```c#

// OnNewRoot = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewRoot, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field OnNewRootPartial</summary><blockquote>

 `On`  or  `Off` . Determines whether to generate a static partial  `OnNewRoot<T>(...)`  method when the  `OnNewRoot`  hint is  `On`  to handle the new composition root registration event.  `On`  by default.
            
```c#

// OnNewRootPartial = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.OnNewRootPartial, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ToString</summary><blockquote>

 `On`  or  `Off` . Determine if the  `ToString()`  method should be generated. This method provides a text-based class diagram in the format mermaid.  `Off`  by default. 
            
```c#

// ToString = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ToString, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ThreadSafe</summary><blockquote>

 `On`  or  `Off` . This hint determines whether object composition will be created in a thread-safe manner.  `On`  by default.
            
```c#

// ThreadSafe = Off
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ThreadSafe, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ResolveMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public T Resolve<T>()` . "Public" by default.
            
```c#

// ResolveMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ResolveMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ResolveMethodName</summary><blockquote>

Overrides name of the method  `public T Resolve<T>()` . "Resolve" by default.
            
```c#

// ResolveMethodName = GetService
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ResolveMethodName, "GetService")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ResolveByTagMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public T Resolve<T>(object? tag)` . "public" by default.
            
```c#

// ResolveByTagMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ResolveByTagMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ResolveByTagMethodName</summary><blockquote>

Overrides name of the method  `public T Resolve<T>(object? tag)` . "Resolve" by default.
            For example
            
```c#

// ResolveByTagMethodName = GetService
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ResolveByTagMethodName, "GetService")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ObjectResolveMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public object Resolve(Type type)` . "public" by default.
            
```c#

// ObjectResolveMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ObjectResolveMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ObjectResolveMethodName</summary><blockquote>

Overrides name of the method  `public object Resolve(Type type)` . "Resolve" by default.
            
```c#

// ObjectResolveMethodName = GetService
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ObjectResolveMethodName, "GetService")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ObjectResolveByTagMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public object Resolve(Type type, object? tag)` . "public" by default.
            
```c#

// ObjectResolveByTagMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ObjectResolveByTagMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field ObjectResolveByTagMethodName</summary><blockquote>

Overrides name of the method  `public object Resolve(Type type, object? tag)` . "Resolve" by default.
            
```c#

// ObjectResolveByTagMethodName = GetService
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.ObjectResolveByTagMethodName, "GetService")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisposeMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public void Dispose()` . "public" by default.
            
```c#

// DisposeMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisposeMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisposeAsyncMethodModifiers</summary><blockquote>

Overrides modifiers of the method  `public _ValueTask_ DisposeAsyncMethodModifiers()` . "public" by default.
            
```c#

// DisposeAsyncMethodModifiers = internal
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisposeAsyncMethodModifiers, "internal")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field FormatCode</summary><blockquote>

 `On`  or  `Off` . Specifies whether the generated code should be formatted. This option consumes a lot of CPU resources.  `Off`  by default.
            
```c#

// FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.FormatCode, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SeverityOfNotImplementedContract</summary><blockquote>

 `Error`  or  `Warning`  or  `Info`  or  `Hidden` . Indicates the severity level of the situation when, in the binding, an implementation does not implement a contract.  `Error`  by default.
            
```c#

// FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.SeverityOfNotImplementedContracts, "On")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field Comments</summary><blockquote>

 `On`  or  `Off` . Specifies whether the generated code should be commented.  `On`  by default.
            
```c#

// Comments = Off
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.Comments, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SkipDefaultConstructor</summary><blockquote>

 `On`  or  `Off` . Determines whether to skip using the default constructor to create an instance.  `Off`  by default.
            
```c#

// SkipDefaultConstructor = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.UseDefaultConstructor, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SkipDefaultConstructorImplementationTypeNameRegularExpression</summary><blockquote>

The regular expression to filter whether to skip using the default constructor to create an instance by the instance type name. ".+" by default.
            
```c#

// SkipDefaultConstructorImplementationTypeNameRegularExpression = Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.SkipDefaultConstructorImplementationTypeNameRegularExpression, "Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SkipDefaultConstructorImplementationTypeNameWildcard</summary><blockquote>

The wildcard to filter whether to skip using the default constructor to create an instance by the instance type name. "*" by default.
            
```c#

// SkipDefaultConstructorImplementationTypeNameWildcard = *Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.SkipDefaultConstructorImplementationTypeNameWildcard, "*Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SkipDefaultConstructorLifetimeRegularExpression</summary><blockquote>

The regular expression to filter whether to skip using the default constructor to create an instance by the lifetime. ".+" by default.
            
```c#

// SkipDefaultConstructorLifetimeRegularExpression = Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.SkipDefaultConstructorLifetimeRegularExpression, "Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field SkipDefaultConstructorLifetimeWildcard</summary><blockquote>

The wildcard to filter whether to skip using the default constructor to create an instance by the lifetime. ".+" by default.
            
```c#

// SkipDefaultConstructorLifetimeWildcard = *Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.SkipDefaultConstructorLifetimeWildcard, "*Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisableAutoBinding</summary><blockquote>

 `On`  or  `Off` . Determines whether dependency injection should NOT be performed if a binding for that dependency has not been explicitly defined.  `Off`  by default.
            
```c#

// DisableAutoBinding = On
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisableAutoBinding, "Off")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisableAutoBindingImplementationTypeNameRegularExpression</summary><blockquote>

The regular expression by the instance type name to filter whether a dependency injection should NOT be performed if a binding for that dependency has not been explicitly defined. ".+" by default.
            
```c#

// DisableAutoBindingImplementationTypeNameRegularExpression = Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisableAutoBindingImplementationTypeNameRegularExpression, "Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisableAutoBindingImplementationTypeNameWildcard</summary><blockquote>

The wildcard by the instance type name to filter whether a dependency injection should NOT be performed if a binding for that dependency has not been explicitly defined. "*" by default.
            
```c#

// DisableAutoBindingImplementationTypeNameWildcard = *Dependency
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisableAutoBindingImplementationTypeNameWildcard, "*Dependency")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisableAutoBindingLifetimeRegularExpression</summary><blockquote>

The regular expression by the lifetime to filter whether a dependency injection should NOT be performed if a binding for that dependency has not been explicitly defined. ".+" by default.
            
```c#

// DisableAutoBindingLifetimeRegularExpression = Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisableAutoBindingLifetimeRegularExpression, "Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


<details><summary>Field DisableAutoBindingLifetimeWildcard</summary><blockquote>

The wildcard by the lifetime to filter whether a dependency injection should NOT be performed if a binding for that dependency has not been explicitly defined. ".+" by default.
            
```c#

// DisableAutoBindingLifetimeWildcard = *Singleton
            DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


            or using the API call _Hint(Pure.DI.Hint,System.String)_:
            
```c#

DI.Setup("Composition")
                .Hint(Hint.DisableAutoBindingLifetimeWildcard, "*Singleton")
                .Bind<IDependency>().To<Dependency>();
            
```


See also _Hint(Pure.DI.Hint,System.String)_.

</blockquote></details>


</blockquote></details>


<details><summary>IBinding</summary><blockquote>

An API for a binding setup.
            
<details><summary>Method Bind(System.Object[])</summary><blockquote>

Begins the binding definition for the implementation type itself, and if the implementation is not an abstract class or structure, for all abstract but NOT special types that are directly implemented.
            Special types include:
            System.ObjectSystem.EnumSystem.MulticastDelegateSystem.DelegateSystem.Collections.IEnumerableSystem.Collections.Generic.IEnumerable<T>System.Collections.Generic.IList<T>System.Collections.Generic.ICollection<T>System.Collections.IEnumeratorSystem.Collections.Generic.IEnumerator<T>System.Collections.Generic.IIReadOnlyList<T>System.Collections.Generic.IReadOnlyCollection<T>System.IDisposableSystem.IAsyncResultSystem.AsyncCallback
```c#

DI.Setup("Composition")
                .Bind().To<Service>();
            
```


 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``1(System.Object[])</summary><blockquote>

Begins the definition of the binding.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```

The type of dependency to be bound. Common type markers such as _TT_, _TTList`1_ and others are also supported.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``2(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``3(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.Type 3 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``4(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.Type 3 of a dependency to be bound.Type 3 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of a dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method As(Pure.DI.Lifetime)</summary><blockquote>

Determines the _Lifetime_ of a binding.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>();
            
```


 - parameter _lifetime_ - The _Lifetime_ of a binding

 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

</blockquote></details>


<details><summary>Method Tags(System.Object[])</summary><blockquote>

Defines the binding tags.
             Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, tags will help:
             
```c#

interface IDependency { }
             
            
             class AbcDependency: IDependency { }
             
            
             class XyzDependency: IDependency { }
             
            
             class Dependency: IDependency { }
             
            
             interface IService
             {
                 IDependency Dependency1 { get; }
             
            
                 IDependency Dependency2 { get; }
             }
            
             
             class Service: IService
             {
                 public Service(
                     [Tag("Abc")] IDependency dependency1,
                     [Tag("Xyz")] IDependency dependency2)
                 {
                     Dependency1 = dependency1;
                     Dependency2 = dependency2;
                 }
            
                 public IDependency Dependency1 { get; }
            
             
                 public IDependency Dependency2 { get; }
             }
            
             
             DI.Setup("Composition")
                 .Bind<IDependency>().Tags("Abc").To<AbcDependency>()
                 .Bind<IDependency>().Tags("Xyz").To<XyzDependency>()
                 .Bind<IService>().To<Service>().Root<IService>("Root");
             
```


 - parameter _tags_ - The binding tags.

 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method To``1</summary><blockquote>

Completes the binding chain by specifying the implementation.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```

The implementation type. Also supports generic type markers such as _TT_, _TTList`1_, and others.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method To``1(System.Func{Pure.DI.IContext,``0})</summary><blockquote>

Completes the binding chain by specifying the implementation using a factory method. It allows you to manually create an instance, call the necessary methods, initialize properties, fields, etc.
             
```c#

DI.Setup("Composition")
                 .Bind<IService>()
                 To(_ =>
                 {
                     var service = new Service("My Service");
                     service.Initialize();
                     return service;
                 })
             
```


             another example:
             
```c#

DI.Setup("Composition")
                 .Bind&lt;IService&gt;()
                 To(ctx =&gt;
                 {
                     ctx.Inject<IDependency>(out var dependency);
                     return new Service(dependency);
                 })
             
```


             and another example:
             
```c#

DI.Setup("Composition")
                 .Bind&lt;IService&gt;()
                 To(ctx =&gt;
                 {
                     // Builds up an instance with all necessary dependencies
                     ctx.Inject<Service>(out var service);
            
             
                     service.Initialize();
                     return service;
                 })
             
```


 - parameter _factory_ - An expression for manually creating and initializing an instance.
The implementation type.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1_.

See also _!:To<T1,T>()_.

See also _!:To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method To``1(System.String)</summary><blockquote>

Completes the binding chain by specifying the implementation using a source code statement.
            
```c#

DI.Setup("Composition")
                .Bind<int>().To<int>("dependencyId")
                .Bind<Func<int, IDependency>>()
                    .To<Func<int, IDependency>>(ctx =>
                        dependencyId =>
                        {
                            ctx.Inject<Dependency>(out var dependency);
                            return dependency;
                        });
            
```


 - parameter _sourceCodeStatement_ - Source code statement
The implementation type.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

</blockquote></details>


<details><summary>Method To``2(System.Func{``0,``1})</summary><blockquote>

Completes the binding chain by specifying the implementation using a simplified factory method. It allows you to manually create an instance, call the necessary methods, initialize properties, fields, etc. Each parameter of this factory method represents a dependency injection. Starting with C# 10, you can also put the _TagAttribute_ in front of the parameter to specify the tag of the injected dependency.
            
```c#

DI.Setup(nameof(Composition))
                .Bind<IDependency>().To((
                    Dependency dependency) =>
                {
                    dependency.Initialize();
                    return dependency;
                });
            
```


            A variant using _TagAttribute_:
            
```c#

DI.Setup(nameof(Composition))
                .Bind<IDependency>().To((
                    [Tag("some tag")] Dependency dependency) =>
                {
                    dependency.Initialize();
                    return dependency;
                });
            
```


 - parameter _factory_ - An expression for manually creating and initializing an instance.
Type #1 of the injected dependency.The implementation type.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To``1_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method To``3(System.Func{``0,``1,``2})</summary><blockquote>

Completes the binding chain by specifying the implementation using a simplified factory method. It allows you to manually create an instance, call the necessary methods, initialize properties, fields, etc. Each parameter of this factory method represents a dependency injection. Starting with C# 10, you can also put the _TagAttribute_ in front of the parameter to specify the tag of the injected dependency.
            
```c#

DI.Setup(nameof(Composition))
                .Bind<IDependency>().To((
                    Dependency dependency,
                    DateTimeOffset time) =>
                {
                    dependency.Initialize(time);
                    return dependency;
                });
            
```


            A variant using _TagAttribute_:
            
```c#

DI.Setup(nameof(Composition))
                .Bind("now datetime").To(_ => DateTimeOffset.Now)
                .Bind<IDependency>().To((
                    Dependency dependency,
                    [Tag("now datetime")] DateTimeOffset time) =>
                {
                    dependency.Initialize(time);
                    return dependency;
                });
            
```


 - parameter _factory_ - An expression for manually creating and initializing an instance.
Type #1 of the injected dependency.Type #2 of the injected dependency.The implementation type.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To``1_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method To``4(System.Func{``0,``1,``2,``3})</summary><blockquote>

Completes the binding chain by specifying the implementation using a simplified factory method. It allows you to manually create an instance, call the necessary methods, initialize properties, fields, etc. Each parameter of this factory method represents a dependency injection. Starting with C# 10, you can also put the _TagAttribute_ in front of the parameter to specify the tag of the injected dependency.
            
 - parameter _factory_ - An expression for manually creating and initializing an instance.
Type #1 of the injected dependency.Type #2 of the injected dependency.Type #3 of the injected dependency.The implementation type.
 - returns Reference to the setup continuation chain.

See also _Bind``1(System.Object[])_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To``1_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


</blockquote></details>


<details><summary>IConfiguration</summary><blockquote>

An API for a Dependency Injection setup.
            
See also _Setup(System.String,Pure.DI.CompositionKind)_.

<details><summary>Method Bind(System.Object[])</summary><blockquote>

Begins the binding definition for the implementation type itself, and if the implementation is not an abstract class or structure, for all abstract but NOT special types that are directly implemented.
            Special types include:
            System.ObjectSystem.EnumSystem.MulticastDelegateSystem.DelegateSystem.Collections.IEnumerableSystem.Collections.Generic.IEnumerable<T>System.Collections.Generic.IList<T>System.Collections.Generic.ICollection<T>System.Collections.IEnumeratorSystem.Collections.Generic.IEnumerator<T>System.Collections.Generic.IIReadOnlyList<T>System.Collections.Generic.IReadOnlyCollection<T>System.IDisposableSystem.IAsyncResultSystem.AsyncCallback
```c#

DI.Setup("Composition")
                .Bind().To<Service>();
            
```


 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``1(System.Object[])</summary><blockquote>

Begins the definition of the binding.
            
```c#

DI.Setup("Composition")
                .Bind<IService>().To<Service>();
            
```

The type of dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``2(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``3(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.Type 3 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Bind``4(System.Object[])</summary><blockquote>

Begins binding definition for multiple dependencies. See _Bind``1(System.Object[])_ for details.
            Type 1 of a dependency to be bound.Type 2 of a dependency to be bound.Type 3 of a dependency to be bound.Type 4 of a dependency to be bound.
 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method RootBind``1(System.String,Pure.DI.RootKinds,System.Object[])</summary><blockquote>

Begins the definition of the binding with _Root``1(System.String,System.Object,Pure.DI.RootKinds)_ applied.
            
```c#

DI.Setup("Composition")
                .RootBind<IService>();
            
```

The type of dependency to be bound.
 - parameter _name_ - Specifies the name of the root of the composition. If the value is empty, a private root will be created, which can be used when calling  `Resolve`  methods.
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the root type without its namespaces.{TYPE}Will be replaced with the full name of the root type.{tag}Will be replaced with the first tag name.

 - parameter _kind_ - The optional argument specifying the kind for the root of the composition.

 - parameter _tags_ - The optional argument that specifies tags for a particular type of dependency binding. If it is is not empty, the first tag is used for the root.

 - returns Reference to the setup continuation chain.

See also _To``1_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _To<T1,T>()_.

See also _To<T1,T2,T>()_.

See also _Tags(System.Object[])_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method DependsOn(System.String[])</summary><blockquote>

Indicates the use of some single or multiple setups as base setups by name.
            
```c#

DI.Setup("Composition")
                .DependsOn(nameof(CompositionBase));
            
```


 - parameter _setupNames_ - A set of names for the basic setups on which this one depends.

 - returns Reference to the setup continuation chain.

See also _Setup(System.String,Pure.DI.CompositionKind)_.

</blockquote></details>


<details><summary>Method GenericTypeArgumentAttribute``1</summary><blockquote>

Specifies a custom generic type argument attribute.
            
```c#

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
            class MyGenericTypeArgumentAttribute: Attribute;
             
            [MyGenericTypeArgument]
            interface TTMy; 
             
            DI.Setup("Composition")
                .GenericTypeAttribute<MyGenericTypeArgumentAttribute>()
                .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>();
            
```

The attribute type.
 - returns Reference to the setup continuation chain.

See also _GenericTypeArgumentAttribute``1_.

</blockquote></details>


<details><summary>Method TypeAttribute``1(System.Int32)</summary><blockquote>

Specifies a custom attribute that overrides the injection type.
            
```c#

DI.Setup("Composition")
                .TypeAttribute<MyTypeAttribute>();
            
```


 - parameter _typeArgumentPosition_ - The optional parameter that specifies the position of the type parameter in the attribute constructor. 0 by default. See predefined attribute _TypeAttribute``1(System.Int32)_.
The attribute type.
 - returns Reference to the setup continuation chain.

See also _TypeAttribute_.

</blockquote></details>


<details><summary>Method TagAttribute``1(System.Int32)</summary><blockquote>

Specifies a tag attribute that overrides the injected tag.
            
```c#

DI.Setup("Composition")
                .TagAttribute<MyTagAttribute>();
            
```


 - parameter _tagArgumentPosition_ - The optional parameter that specifies the position of the tag parameter in the attribute constructor. 0 by default. See the predefined _TagAttribute``1(System.Int32)_ attribute.
The attribute type.
 - returns Reference to the setup continuation chain.

See also _TagAttribute_.

</blockquote></details>


<details><summary>Method OrdinalAttribute``1(System.Int32)</summary><blockquote>

Specifies a custom attribute that overrides the injection ordinal.
            
```c#

DI.Setup("Composition")
                .OrdinalAttribute<MyOrdinalAttribute>();
            
```


 - parameter _ordinalArgumentPosition_ - The optional parameter that specifies the position of the ordinal parameter in the attribute constructor. 0 by default. See the predefined _OrdinalAttribute``1(System.Int32)_ attribute.
The attribute type.
 - returns Reference to the setup continuation chain.

See also _OrdinalAttribute_.

</blockquote></details>


<details><summary>Method DefaultLifetime(Pure.DI.Lifetime)</summary><blockquote>

Overrides the default _Lifetime_ for all bindings further down the chain. If not specified, the _Transient_ lifetime is used.
            
```c#

DI.Setup("Composition")
                .DefaultLifetime(Lifetime.Singleton);
            
```


 - parameter _lifetime_ - The default lifetime.

 - returns Reference to the setup continuation chain.

See also _Lifetime_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method DefaultLifetime``1(Pure.DI.Lifetime,System.Object[])</summary><blockquote>

Overrides the default _Lifetime_ for all bindings can be casted to type  further down the chain.
            
```c#

DI.Setup("Composition")
                .DefaultLifetime<IMySingleton>(Lifetime.Singleton);
            
```


```c#

DI.Setup("Composition")
                .DefaultLifetime<IMySingleton>(Lifetime.Singleton, "my tag");
            
```


 - parameter _lifetime_ - The default lifetime.

 - parameter _tags_ - The optional argument specifying the binding tags for which it will set the default lifetime. If not specified, the default lifetime will be set for any tags.
The default lifetime will be applied to bindings if the implementation class can be cast to type .
 - returns Reference to the setup continuation chain.

See also _Lifetime_.

See also _As(Pure.DI.Lifetime)_.

</blockquote></details>


<details><summary>Method Arg``1(System.String,System.Object[])</summary><blockquote>

Adds a partial class argument and replaces the default constructor by adding this argument as a parameter. It is only created if this argument is actually used. 
            
```c#

DI.Setup("Composition")
                .Arg<int>("id");
            
```


 - parameter _name_ - The argument name.
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the argument type without its namespaces.{TYPE}Will be replaced with the full name of the argument type.{tag}Will be replaced with the first tag name.

 - parameter _tags_ - The optional argument that specifies the tags for the argument.
The argument type.
 - returns Reference to the setup continuation chain.

See also _RootArg``1(System.String,System.Object[])_.

</blockquote></details>


<details><summary>Method RootArg``1(System.String,System.Object[])</summary><blockquote>

Adds a root argument to use as a root parameter. 
            
```c#

DI.Setup("Composition")
                .RootArg<int>("id");
            
```


 - parameter _name_ - The argument name.
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the argument type without its namespaces.{TYPE}Will be replaced with the full name of the argument type.{tag}Will be replaced with the first tag name.

 - parameter _tags_ - The optional argument that specifies the tags for the argument.
The argument type.
 - returns Reference to the setup continuation chain.

See also _Arg``1(System.String,System.Object[])_.

</blockquote></details>


<details><summary>Method Root``1(System.String,System.Object,Pure.DI.RootKinds)</summary><blockquote>

Specifying the root of the composition.
            
```c#

DI.Setup("Composition")
                .Root<Service>("MyService");
            
```


```c#

DI.Setup("Composition")
                .Root<Service>("My{type}");
            
```


 - parameter _name_ - Specifies the name of the root of the composition. If the value is empty, a private root will be created, which can be used when calling  `Resolve`  methods.
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the root type without its namespaces.{TYPE}Will be replaced with the full name of the root type.{tag}Will be replaced with the root tag name.

 - parameter _tag_ - The optional argument specifying the tag for the root of the composition.

 - parameter _kind_ - The optional argument specifying the kind for the root of the composition.
The composition root type.
 - returns Reference to the setup continuation chain.

See also _RootBind``1(System.String,Pure.DI.RootKinds,System.Object[])_.

See also _Roots``1(System.String,Pure.DI.RootKinds,System.String)_.

</blockquote></details>


<details><summary>Method Roots``1(System.String,Pure.DI.RootKinds,System.String)</summary><blockquote>

Specifies to define composition roots for all types inherited from _!:T_ available at compile time at the point where the method is called.
            
```c#

DI.Setup("Composition")
                .Roots<IService>();
            
```


```c#

DI.Setup("Composition")
                .Roots<IService>("Root{type}", filter: "*MyService");
            
```


 - parameter _name_ - Specifies the name of the roots of the composition. If the value is empty, private roots will be created, which can be used when calling  `Resolve`  methods.
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the type without its namespaces.{TYPE}Will be replaced with the full name of the type.

 - parameter _kind_ - The optional argument specifying the kind for the root of the composition.

 - parameter _filter_ - A wildcard to filter root types by their full name.
The composition root base type.
 - returns Reference to the setup continuation chain.

See also _Root``1(System.String,System.Object,Pure.DI.RootKinds)_.

</blockquote></details>


<details><summary>Method Builder``1(System.String,Pure.DI.RootKinds)</summary><blockquote>

Specifies the method of the composition builder. The first argument to the method will always be the instance to be built. The remaining arguments to this method will be listed in the order in which they are defined in the setup.Specifies to create a composition builder method. The first argument to the method will always be the instance to be built. The remaining arguments to this method will be listed in the order in which they are defined in the setup.
            
```c#

DI.Setup("Composition")
                .Builder<Service>("BuildUpMyService");
            
```


 - parameter _name_ - Specifies the name of the builder. The default name is "BuildUp".
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the type without its namespaces.{TYPE}Will be replaced with the full name of the type.

 - parameter _kind_ - The optional argument specifying the kind for the root of the composition.
The composition root type.
 - returns Reference to the setup continuation chain.

See also _Builders``1(System.String,Pure.DI.RootKinds,System.String)_.

</blockquote></details>


<details><summary>Method Builders``1(System.String,Pure.DI.RootKinds,System.String)</summary><blockquote>

Specifies to define builders for all types inherited from _!:T_ available at compile time at the point where the method is called.
            
```c#

DI.Setup("Composition")
                .Builders<Service>();
            
```


```c#

DI.Setup("Composition")
                .Builder<Service>("BuildUp");
            
```


```c#

DI.Setup("Composition")
                .Builder<Service>("BuildUp{type}", filter: "*MyService");
            
```


 - parameter _name_ - Specifies the name of the builders. The default name is "BuildUp".
            The name supports templating:
            TemplateDescription{type}Will be replaced by the short name of the type without its namespaces.{TYPE}Will be replaced with the full name of the type.

 - parameter _kind_ - The optional argument specifying the kind for the root of the composition.

 - parameter _filter_ - A wildcard to filter builder types by their full name.
The composition root base type.
 - returns Reference to the setup continuation chain.

See also _Builder``1(System.String,Pure.DI.RootKinds)_.

</blockquote></details>


<details><summary>Method Hint(Pure.DI.Hint,System.String)</summary><blockquote>

Defines a hint for fine-tuning code generation.
            
```c#

DI.Setup("Composition")
                .Hint(Resolve, "Off");
            
```


 - parameter _hint_ - The hint type.

 - parameter _value_ - The hint value.

 - returns Reference to the setup continuation chain.

See also _Hint_.

</blockquote></details>


<details><summary>Method Accumulate``2(Pure.DI.Lifetime[])</summary><blockquote>

Registers an accumulator for instances.
            
```c#

DI.Setup("Composition")
                .Accumulate<IDisposable, MyAccumulator>(Lifetime.Transient);
            
```


 - parameter _lifetimes_ - _Lifetime_ of the instances to be accumulated. Instances with lifetime _Singleton_, _Scoped_, or _PerResolve_ only accumulate in an accumulator that is NOT lazily created.
The type of instance. All instances that can be cast to this type will be accumulated.The type of accumulator. It must have a public constructor without parameters and a  `Add`  method with a single argument that allows you to add an instance of type .
 - returns Reference to the setup continuation chain.

See also _Lifetime_.

</blockquote></details>


<details><summary>Method GenericTypeArgument``1</summary><blockquote>

Specifies a custom generic type argument.
            
```c#

interface TTMy;
             
            DI.Setup("Composition")
                .GenericTypeArgument<TTMy>()
                .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>();
            
```

The generic type marker.
 - returns Reference to the setup continuation chain.

See also _GenericTypeArgumentAttribute``1_.

</blockquote></details>


</blockquote></details>


<details><summary>IContext</summary><blockquote>

Injection context. Cannot be used outside the binding setup.
            
<details><summary>Property Tag</summary><blockquote>

The tag that was used to inject the current object in the object graph. Cannot be used outside the binding setup. See also _Tags(System.Object[])_
```c#

DI.Setup("Composition")
                .Bind<Lazy<TT>>()
                .To(ctx =>
                {
                    ctx.Inject<Func<TT>>(ctx.Tag, out var func);
                    return new Lazy<TT>(func, false);
                };
            
```


See also _To``1(System.Func{Pure.DI.IContext,``0})_.

See also _Tags(System.Object[])_.

</blockquote></details>


<details><summary>Property ConsumerTypes</summary><blockquote>

The chain of consumer types for which an instance is created, from the immediate consumer down to the composition type. Cannot be used outside the binding setup. Guaranteed to contain at least one element.
             
```c#

var box = new Composition().Box;
             // Output: ShroedingersCat, CardboardBox`1, Composition
            
            
             static void Setup() =>
                 DI.Setup(nameof(Composition))
                 .Bind().To(ctx => new Log(ctx.ConsumerTypes))
                 .Bind().To<ShroedingersCat>()
                 .Bind().To<CardboardBox<TT>>()
                 .Root<CardboardBox<ShroedingersCat>>("Box");
            
            
             public class Log
             {
                 public Log(Type[] types) =>
                     Console.WriteLine(string.Join(", ", types.Select(type => type.Name)));
             }
            
            
             public record CardboardBox<T>(T Content);
            
            
             public record ShroedingersCat(Log log);
             
```


See also _ConsumerType_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

</blockquote></details>


<details><summary>Property ConsumerType</summary><blockquote>

The immediate consumer type for which the instance is created. Cannot be used outside the binding setup.
             
```c#

var box = new Composition().Box;
             // Output: ShroedingersCat
            
            
             static void Setup() =>
                 DI.Setup(nameof(Composition))
                 .Bind().To(ctx => new Log(ctx.ConsumerType))
                 .Bind().To<ShroedingersCat>()
                 .Bind().To<CardboardBox<TT>>()
                 .Root<CardboardBox<ShroedingersCat>>("Box");
            
            
             public class Log
             {
                 public Log(Type type) =>
                     Console.WriteLine(type.Name);
             }
            
            
             public record CardboardBox<T>(T Content);
            
            
             public record ShroedingersCat(Log log);
             
```


See also _ConsumerTypes_.

See also _To``1(System.Func{Pure.DI.IContext,``0})_.

</blockquote></details>


<details><summary>Method Inject``1(``0@)</summary><blockquote>

Injects an instance of type  `T` . Cannot be used outside the binding setup.
             
```c#

DI.Setup("Composition")
                 .Bind<IService>()
                 To(ctx =>
                 {
                     ctx.Inject<IDependency>(out var dependency);
                     return new Service(dependency);
                 })
             
```


             and another example:
```c#

DI.Setup("Composition")
                 .Bind<IService>()
                 To(ctx =>
                 {
                     // Builds up an instance with all necessary dependencies
                     ctx.Inject<Service>(out var service);
            
             
                     service.Initialize();
                     return service;
                 })
             
```


 - parameter _value_ - Injectable instance.
.
             Instance type.
See also _To``1(System.Func{Pure.DI.IContext,``0})_.

</blockquote></details>


<details><summary>Method Inject``1(System.Object,``0@)</summary><blockquote>

Injects an instance of type  `T`  marked with a tag. Cannot be used outside the binding setup.
            
```c#

DI.Setup("Composition")
                .Bind<IService>()
                To(ctx =>
                {
                    ctx.Inject<IDependency>("MyTag", out var dependency);
                    return new Service(dependency);
                })
            
```


 - parameter _tag_ - The injection tag. See also _Tags(System.Object[])_
.
            
 - parameter _value_ - Injectable instance.
.
            Instance type.
See also _To``1(System.Func{Pure.DI.IContext,``0})_.

</blockquote></details>


<details><summary>Method BuildUp``1(``0)</summary><blockquote>

Builds up of an existing object. In other words, injects the necessary dependencies via methods, properties, or fields into an existing object. Cannot be used outside the binding setup.
            
```c#

DI.Setup("Composition")
                .Bind<IService>()
                To(ctx =>
                {
                    var service = new Service();
                    // Initialize an instance with all necessary dependencies
                    ctx.BuildUp(service);
                    return service;
                })
            
```


 - parameter _value_ - An existing object for which the injection(s) is to be performed.
Object type.
See also _To``1(System.Func{Pure.DI.IContext,``0})_.

</blockquote></details>


<details><summary>Method Override``1(``0,System.Object[])</summary><blockquote>

Overrides the binding with the specified value. Cannot be used outside the binding setting.
            
```c#

DI.Setup("Composition")
                .Bind().To<Func<int, int, IDependency>>(ctx =>
                    (dependencyId, subId) =>
                    {
                        // Overrides with a lambda argument
                        ctx.Override(dependencyId);
                        // Overrides with tag using lambda argument
                        ctx.Override(subId, "sub");
                        // Overrides with some value
                        ctx.Override($"Dep {dependencyId} {subId}");
                        // Overrides with injected value
                        ctx.Inject(Tag.Red, out Color red);
                        ctx.Override(red);
                        ctx.Inject<Dependency>(out var dependency);
                        return dependency;
                    })
            
```


            Overrides uses a shared state to override values. And if this code is supposed to run in multiple threads at once, then you need to ensure their synchronization, for example
            
```c#

DI.Setup("Composition")
                .Bind().To<Func<int, int, IDependency>>(ctx =>
                    (dependencyId, subId) =>
                    {
                        // Get composition sync root object
                        ctx.Inject(Tag.SyncRoot, out Lock lockObject);
                        lock(lockObject)
                        {
                            // Overrides with a lambda argument
                            ctx.Override(dependencyId);
                            // Overrides with tag using lambda argument
                            ctx.Override(subId, "sub");
                            // Overrides with some value
                            ctx.Override($"Dep {dependencyId} {subId}");
                            // Overrides with injected value
                            ctx.Inject(Tag.Red, out Color red);
                            ctx.Override(red);
                            ctx.Inject<Dependency>(out var dependency);
                            return dependency;
                        }
                    })
            
```


            An alternative to synchronizing streams yourself is to use types like _Func`3_this. There, threads synchronization is performed automatically.
            
 - parameter _value_ - The object that will be used to override a binding.
Object type that will be used to override a binding.
 - parameter _tags_ - Injection tags that will be used to override a binding. See also _Tags(System.Object[])_
.
            
See also _To<T>(System.Func<TArg1,T>)_.

</blockquote></details>


</blockquote></details>


<details><summary>IOwned</summary><blockquote>

This abstraction allows a disposable object to be disposed of.
            
See also _Owned_.

See also _Accumulate``2(Pure.DI.Lifetime[])_.

</blockquote></details>


<details><summary>Lifetime</summary><blockquote>

Binding lifetimes.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>();
            
```


See also _Setup(System.String,Pure.DI.CompositionKind)_.

See also _As(Pure.DI.Lifetime)_.

See also _DefaultLifetime(Pure.DI.Lifetime)_.

See also _DefaultLifetime``1(Pure.DI.Lifetime,System.Object[])_.

<details><summary>Field Transient</summary><blockquote>

Specifies to create a new dependency instance each time. This is the default value and can be omitted.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Transient).To<Dependency>();
            
```


            This is the default lifetime, it can be omitted, for example,
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().To<Dependency>();
            
```


</blockquote></details>


<details><summary>Field Singleton</summary><blockquote>

Ensures that there will be a single instance of the dependency for each composition.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>();
            
```


</blockquote></details>


<details><summary>Field PerResolve</summary><blockquote>

Guarantees that there will be a single instance of the dependency for each root of the composition.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>();
            
```


</blockquote></details>


<details><summary>Field PerBlock</summary><blockquote>

Does not guarantee that there will be a single instance of the dependency for each root of the composition, but is useful to reduce the number of instances created.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.PerBlock).To<Dependency>();
            
```


</blockquote></details>


<details><summary>Field Scoped</summary><blockquote>

Ensures that there will be a single instance of the dependency for each scope.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency>().As(Lifetime.Scoped).To<Dependency>();
            
```


</blockquote></details>


</blockquote></details>


<details><summary>Name</summary><blockquote>

Represents well-known names.
            
</blockquote></details>


<details><summary>OrdinalAttribute</summary><blockquote>

Represents an ordinal attribute.
             This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
             For constructors, it defines the sequence of attempts to use a particular constructor to create an object:
             
```c#

class Service: IService
             {
                 private readonly string _name;
             
            
                 [Ordinal(1)]
                 public Service(IDependency dependency) =>
                     _name = "with dependency";
             
            
                 [Ordinal(0)]
                 public Service(string name) => _name = name;
             }
             
```


             For fields, properties and methods, it specifies to perform dependency injection and defines the sequence:
             
```c#

class Person: IPerson
             {
                 private readonly string _name = "";
             
                 [Ordinal(0)]
                 public int Id;
            
             
                 [Ordinal(1)]
                 public string FirstName
                 {
                     set
                     {
                         _name = value;
                     }
                 }
             
            
                 public IDependency? Dependency { get; private set; }
             
            
                 [Ordinal(2)]
                 public void SetDependency(IDependency dependency) =>
                     Dependency = dependency;
             }
             
```


See also _DependencyAttribute_.

See also _TagAttribute_.

See also _TypeAttribute_.

<details><summary>Constructor OrdinalAttribute(System.Int32)</summary><blockquote>

Creates an attribute instance.
            
 - parameter _ordinal_ - The injection ordinal.

</blockquote></details>


</blockquote></details>


<details><summary>Owned</summary><blockquote>

Performs accumulation and disposal of disposable objects.
            
See also _IOwned_.

See also _Accumulate``2(Pure.DI.Lifetime[])_.

<details><summary>Method Dispose</summary><blockquote>


</blockquote></details>


</blockquote></details>


<details><summary>Owned`1</summary><blockquote>

Contains a value and gives the ability to dispose of that value.
            Type of value owned.
See also _IOwned_.

See also _Owned_.

See also _Accumulate``2(Pure.DI.Lifetime[])_.

<details><summary>Field Value</summary><blockquote>

Own value.
            
</blockquote></details>


<details><summary>Constructor Owned`1(`0,Pure.DI.IOwned)</summary><blockquote>

Creates a new instance.
            
 - parameter _value_ - Own value.

 - parameter _owned_ - The abstraction allows a disposable object to be disposed of.

</blockquote></details>


<details><summary>Method Dispose</summary><blockquote>


</blockquote></details>


</blockquote></details>


<details><summary>Pair`1</summary><blockquote>

For internal use.
            
</blockquote></details>


<details><summary>RootKinds</summary><blockquote>

Determines a kind of root of the composition.
            
See also _Root``1(System.String,System.Object,Pure.DI.RootKinds)_.

See also _RootBind``1(System.String,Pure.DI.RootKinds,System.Object[])_.

See also _Roots``1(System.String,Pure.DI.RootKinds,System.String)_.

See also _Builder``1(System.String,Pure.DI.RootKinds)_.

See also _Builders``1(System.String,Pure.DI.RootKinds,System.String)_.

<details><summary>Field Default</summary><blockquote>

Specifies to use the default composition root kind.
            
</blockquote></details>


<details><summary>Field Public</summary><blockquote>

Specifies to use a  `public`  access modifier for the root of the composition.
            
</blockquote></details>


<details><summary>Field Internal</summary><blockquote>

Specifies to use a  `internal`  access modifier for the root of the composition.
            
</blockquote></details>


<details><summary>Field Private</summary><blockquote>

Specifies to use a  `private`  access modifier for the root of the composition.
            
</blockquote></details>


<details><summary>Field Property</summary><blockquote>

Specifies to create a composition root as a property.
            
</blockquote></details>


<details><summary>Field Method</summary><blockquote>

Specifies to create a composition root as a method.
            
</blockquote></details>


<details><summary>Field Static</summary><blockquote>

Specifies to create a static root of the composition.
            
</blockquote></details>


<details><summary>Field Partial</summary><blockquote>

Specifies to create a partial root of the composition.
            
</blockquote></details>


<details><summary>Field Exposed</summary><blockquote>

Specifies to create an exposed root of the composition.
            
See also _BindAttribute_.

</blockquote></details>


<details><summary>Field Protected</summary><blockquote>

Specifies to use a  `protected`  access modifier for the root of the composition.
            
</blockquote></details>


<details><summary>Field Virtual</summary><blockquote>

Specifies to use a  `virtual`  modifier for the root of the composition.
            
</blockquote></details>


<details><summary>Field Override</summary><blockquote>

Specifies to use a  `override`  modifier for the root of the composition.
            
</blockquote></details>


</blockquote></details>


<details><summary>Tag</summary><blockquote>

Represents well-known tags.
            
See also _Bind``1(System.Object[])_.

See also _Tags(System.Object[])_.

<details><summary>Field Unique</summary><blockquote>

Unique tag.
            Begins the definition of the binding.
            
```c#

DI.Setup("Composition")
                .Bind<IService>(Tag.Unique).To<Service1>()
                .Bind<IService>(Tag.Unique).To<Service1>()
                .Root<IEnumerable<IService>>("Root");
            
```


</blockquote></details>


<details><summary>Field Type</summary><blockquote>

Tag of a target implementation type.
            
```c#

DI.Setup("Composition")
                .Bind<IService>(Tag.Type).To<Service>()
                .Root<IService>("Root", typeof(Service));
            
```


</blockquote></details>


<details><summary>Field Any</summary><blockquote>

Any tag.
            
```c#

DI.Setup("Composition")
             DI.Setup(nameof(Composition))
                 .Bind<IDependency>(Tag.Any).To(ctx => new Dependency(ctx.Tag))
                 .Bind<IService>().To<Service>()
            
```


</blockquote></details>


<details><summary>Method On(System.String[])</summary><blockquote>

This tag allows you to determine which binding will be used for explicit injection for a particular injection site.
            
```c#

DI.Setup("Composition")
                .Bind(Tag.On("MyNamespace.Service.Service:dep"))
                    .To<Dependency>()
                .Bind().To<Service>()
                .Root<IService>("Root");
            
```


 - parameter _injectionSites_ - Set of labels for injection each must be specified in a special format: <namespace>.<type>.<member>[:argument]. The argument is specified only for the constructor and methods. The wildcards '*' and '?' are supported. All names are case-sensitive. The global namespace prefix 'global::' must be omitted.

</blockquote></details>


<details><summary>Method OnConstructorArg``1(System.String)</summary><blockquote>

This tag allows you to determine which binding will be used for explicit injection for a particular constructor argument.
            
```c#

DI.Setup("Composition")
                .Bind(Tag.OnConstructorArg<Service>("dep"))
                    .To<Dependency>()
                .Bind().To<Service>()
                .Root<IService>("Root");
            
```


 - parameter _argName_ - The name of the constructor argument.

</blockquote></details>


<details><summary>Method OnMember``1(System.String)</summary><blockquote>

This tag allows you to define which binding will be used for explicit injection for a property or field of the type.
            
```c#

DI.Setup("Composition")
                .Bind(Tag.OnMember<Service>("DepProperty"))
                    .To<Dependency>()
                .Bind().To<Service>()
                .Root<IService>("Root");
            
```


 - parameter _memberName_ - The name of the type member.

</blockquote></details>


<details><summary>Method OnMethodArg``1(System.String,System.String)</summary><blockquote>

This tag allows you to determine which binding will be used for explicit injection for a particular method argument.
            
```c#

DI.Setup("Composition")
                .Bind(Tag.OnMethodArg<Service>("DoSomething", "state"))
                    .To<Dependency>()
                .Bind().To<Service>()
                .Root<IService>("Root");
            
```


 - parameter _methodName_ - The name of the type method.

 - parameter _argName_ - The name of the method argument.

</blockquote></details>


<details><summary>Field SyncRoot</summary><blockquote>

Atomically generated smart tag with value "SyncRoot".
            It's used for:
            
            class _Generator__Func{T, TResult}_ <-- (SyncRoot) -- _T:object_ as _Transient__Func{T1, T2, TResult}_ <-- (SyncRoot) -- _T:object_ as _Transient_
</blockquote></details>


<details><summary>Field UniqueTag</summary><blockquote>

Atomically generated smart tag with value "UniqueTag".
            It's used for:
            
            class _Generator__ApiInvocationProcessor_ <-- (UniqueTag) -- _IdGenerator_ as _PerResolve__BindingBuilder_ <-- _IIdGenerator_(UniqueTag) -- _IdGenerator_ as _PerResolve_
</blockquote></details>


<details><summary>Field VarName</summary><blockquote>

Atomically generated smart tag with value "VarName".
            It's used for:
            
            class _Generator__VarsMap_ <-- _IIdGenerator_(VarName) -- _IdGenerator_ as _Transient_
</blockquote></details>


<details><summary>Field CompositionClass</summary><blockquote>

Atomically generated smart tag with value "CompositionClass".
            It's used for:
            
            class _Generator__CodeBuilder_ <-- _IBuilder{TData, T}_(CompositionClass) -- _CompositionClassBuilder_ as _PerBlock_
</blockquote></details>


<details><summary>Field UsingDeclarations</summary><blockquote>

Atomically generated smart tag with value "UsingDeclarations".
            It's used for:
            
            class _Generator__CompositionClassBuilder_ <-- _IBuilder{TData, T}_(UsingDeclarations) -- _UsingDeclarationsBuilder_ as _PerBlock_
</blockquote></details>


<details><summary>Field Override</summary><blockquote>

Atomically generated smart tag with value "Override".
            It's used for:
            
            class _Generator__OverrideIdProvider_ <-- _IIdGenerator_(Override) -- _IdGenerator_ as _PerResolve_
</blockquote></details>


<details><summary>Field Overrider</summary><blockquote>

Atomically generated smart tag with value "Overrider".
            It's used for:
            
            class _Generator__DependencyGraphBuilder_ <-- _IGraphRewriter_(Overrider) -- _GraphOverrider_ as _PerBlock_
</blockquote></details>


<details><summary>Field Cleaner</summary><blockquote>

Atomically generated smart tag with value "Cleaner".
            It's used for:
            
            class _Generator__DependencyGraphBuilder_ <-- _IGraphRewriter_(Cleaner) -- _GraphCleaner_ as _PerBlock_
</blockquote></details>


</blockquote></details>


<details><summary>TagAttribute</summary><blockquote>

Represents a tag attribute overriding an injection tag. The tag can be a constant, a type, or a value of an enumerated type.
             This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
             Sometimes it's important to take control of building a dependency graph. For example, when there are multiple implementations of the same contract. In this case, tags will help:
             
```c#

interface IDependency { }
             
            
             class AbcDependency: IDependency { }
             
            
             class XyzDependency: IDependency { }
             
            
             class Dependency: IDependency { }
             
            
             interface IService
             {
                 IDependency Dependency1 { get; }
             
            
                 IDependency Dependency2 { get; }
             }
            
             
             class Service: IService
             {
                 public Service(
                     [Tag("Abc")] IDependency dependency1,
                     [Tag("Xyz")] IDependency dependency2)
                 {
                     Dependency1 = dependency1;
                     Dependency2 = dependency2;
                 }
            
                 public IDependency Dependency1 { get; }
            
             
                 public IDependency Dependency2 { get; }
             }
            
             
             DI.Setup("Composition")
                 .Bind<IDependency>("Abc").To<AbcDependency>()
                 .Bind<IDependency>("Xyz").To<XyzDependency>()
                 .Bind<IService>().To<Service>().Root<IService>("Root");
             
```


See also _DependencyAttribute_.

See also _OrdinalAttribute_.

See also _TypeAttribute_.

<details><summary>Constructor TagAttribute(System.Object)</summary><blockquote>

Creates an attribute instance.
            
 - parameter _tag_ - The injection tag. See also _Tags(System.Object[])_
.
        
</blockquote></details>


</blockquote></details>


<details><summary>TT</summary><blockquote>

Represents the generic type arguments marker for a reference type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TT>>().To<Dependency<TT>>();
            
```


</blockquote></details>


<details><summary>TT1</summary><blockquote>

Represents the generic type arguments marker for a reference type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TT1>>().To<Dependency<TT1>>();
            
```


</blockquote></details>


<details><summary>TT2</summary><blockquote>

Represents the generic type arguments marker for a reference type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TT2>>().To<Dependency<TT2>>();
            
```


</blockquote></details>


<details><summary>TT3</summary><blockquote>

Represents the generic type arguments marker for a reference type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TT3>>().To<Dependency<TT3>>();
            
```


</blockquote></details>


<details><summary>TT4</summary><blockquote>

Represents the generic type arguments marker for a reference type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TT4>>().To<Dependency<TT4>>();
            
```


</blockquote></details>


<details><summary>TTCollection`1</summary><blockquote>

Represents the generic type arguments marker for _ICollection`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTCollection<TT>>>().To<Dependency<TTCollection<TT>>>();
            
```


</blockquote></details>


<details><summary>TTCollection1`1</summary><blockquote>

Represents the generic type arguments marker for _ICollection`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTCollection1<TT>>>().To<Dependency<TTCollection1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTCollection2`1</summary><blockquote>

Represents the generic type arguments marker for _ICollection`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTCollection2<TT>>>().To<Dependency<TTCollection2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTCollection3`1</summary><blockquote>

Represents the generic type arguments marker for _ICollection`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTCollection3<TT>>>().To<Dependency<TTCollection3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTCollection4`1</summary><blockquote>

Represents the generic type arguments marker for _ICollection`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTCollection4<TT>>>().To<Dependency<TTCollection4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparable</summary><blockquote>

Represents the generic type arguments marker for _IComparable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable>>().To<Dependency<TTComparable>>();
            
```


</blockquote></details>


<details><summary>TTComparable`1</summary><blockquote>

Represents the generic type arguments marker for _IComparable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable<TT>>>().To<Dependency<TTComparable<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparable1</summary><blockquote>

Represents the generic type arguments marker for _IComparable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable1>>().To<Dependency<TTComparable1>>();
            
```


</blockquote></details>


<details><summary>TTComparable1`1</summary><blockquote>

Represents the generic type arguments marker for _IComparable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable1<TT>>>().To<Dependency<TTComparable1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparable2</summary><blockquote>

Represents the generic type arguments marker for _IComparable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable2>>().To<Dependency<TTComparable2>>();
            
```


</blockquote></details>


<details><summary>TTComparable2`1</summary><blockquote>

Represents the generic type arguments marker for _IComparable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable2<TT>>>().To<Dependency<TTComparable2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparable3</summary><blockquote>

Represents the generic type arguments marker for _IComparable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable3>>().To<Dependency<TTComparable3>>();
            
```


</blockquote></details>


<details><summary>TTComparable3`1</summary><blockquote>

Represents the generic type arguments marker for _IComparable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable3<TT>>>().To<Dependency<TTComparable3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparable4</summary><blockquote>

Represents the generic type arguments marker for _IComparable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable4>>().To<Dependency<TTComparable4>>();
            
```


</blockquote></details>


<details><summary>TTComparable4`1</summary><blockquote>

Represents the generic type arguments marker for _IComparable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparable4<TT>>>().To<Dependency<TTComparable4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparer`1</summary><blockquote>

Represents the generic type arguments marker for _IComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparer<TT>>>().To<Dependency<TTComparer<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparer1`1</summary><blockquote>

Represents the generic type arguments marker for _IComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparer1<TT>>>().To<Dependency<TTComparer1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparer2`1</summary><blockquote>

Represents the generic type arguments marker for _IComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparer2<TT>>>().To<Dependency<TTComparer2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparer3`1</summary><blockquote>

Represents the generic type arguments marker for _IComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparer3<TT>>>().To<Dependency<TTComparer3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTComparer4`1</summary><blockquote>

Represents the generic type arguments marker for _IComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTComparer4<TT>>>().To<Dependency<TTComparer4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTDisposable</summary><blockquote>

Represents the generic type arguments marker for _IDisposable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTDisposable>>().To<Dependency<TTDisposable>>();
            
```


</blockquote></details>


<details><summary>TTDisposable1</summary><blockquote>

Represents the generic type arguments marker for _IDisposable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTDisposable1>>().To<Dependency<TTDisposable1>>();
            
```


</blockquote></details>


<details><summary>TTDisposable2</summary><blockquote>

Represents the generic type arguments marker for _IDisposable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTDisposable2>>().To<Dependency<TTDisposable2>>();
            
```


</blockquote></details>


<details><summary>TTDisposable3</summary><blockquote>

Represents the generic type arguments marker for _IDisposable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTDisposable3>>().To<Dependency<TTDisposable3>>();
            
```


</blockquote></details>


<details><summary>TTDisposable4</summary><blockquote>

Represents the generic type arguments marker for _IDisposable_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTDisposable4>>().To<Dependency<TTDisposable4>>();
            
```


</blockquote></details>


<details><summary>TTE</summary><blockquote>

Represents the generic type arguments marker for a enum type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTE>>().To<Dependency<TTE>>();
            
```


</blockquote></details>


<details><summary>TTE1</summary><blockquote>

Represents the generic type arguments marker for a enum type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTE1>>().To<Dependency<TTE1>>();
            
```


</blockquote></details>


<details><summary>TTE2</summary><blockquote>

Represents the generic type arguments marker for a enum type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTE2>>().To<Dependency<TTE2>>();
            
```


</blockquote></details>


<details><summary>TTE3</summary><blockquote>

Represents the generic type arguments marker for a enum type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTE3>>().To<Dependency<TTE3>>();
            
```


</blockquote></details>


<details><summary>TTE4</summary><blockquote>

Represents the generic type arguments marker for a enum type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTE4>>().To<Dependency<TTE4>>();
            
```


</blockquote></details>


<details><summary>TTEnumerable`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerable<TT>>>().To<Dependency<TTEnumerable<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerable1`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerable1<TT>>>().To<Dependency<TTEnumerable1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerable2`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerable2<TT>>>().To<Dependency<TTEnumerable2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerable3`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerable3<TT>>>().To<Dependency<TTEnumerable3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerable4`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerable4<TT>>>().To<Dependency<TTEnumerable4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerator`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerator`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerator<TT>>>().To<Dependency<TTEnumerator<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerator1`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerator`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerator1<TT>>>().To<Dependency<TTEnumerator1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerator2`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerator`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerator2<TT>>>().To<Dependency<TTEnumerator2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerator3`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerator`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerator3<TT>>>().To<Dependency<TTEnumerator3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEnumerator4`1</summary><blockquote>

Represents the generic type arguments marker for _IEnumerator`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEnumerator4<TT>>>().To<Dependency<TTEnumerator4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEqualityComparer`1</summary><blockquote>

Represents the generic type arguments marker for _IEqualityComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEqualityComparer<TT>>>().To<Dependency<TTEqualityComparer<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEqualityComparer1`1</summary><blockquote>

Represents the generic type arguments marker for _IEqualityComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEqualityComparer1<TT>>>().To<Dependency<TTEqualityComparer1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEqualityComparer2`1</summary><blockquote>

Represents the generic type arguments marker for _IEqualityComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEqualityComparer2<TT>>>().To<Dependency<TTEqualityComparer2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEqualityComparer3`1</summary><blockquote>

Represents the generic type arguments marker for _IEqualityComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEqualityComparer3<TT>>>().To<Dependency<TTEqualityComparer3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEqualityComparer4`1</summary><blockquote>

Represents the generic type arguments marker for _IEqualityComparer`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEqualityComparer4<TT>>>().To<Dependency<TTEqualityComparer4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEquatable`1</summary><blockquote>

Represents the generic type arguments marker for _IEquatable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEquatable<TT>>>().To<Dependency<TTEquatable<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEquatable1`1</summary><blockquote>

Represents the generic type arguments marker for _IEquatable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEquatable1<TT>>>().To<Dependency<TTEquatable1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEquatable2`1</summary><blockquote>

Represents the generic type arguments marker for _IEquatable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEquatable2<TT>>>().To<Dependency<TTEquatable2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEquatable3`1</summary><blockquote>

Represents the generic type arguments marker for _IEquatable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEquatable3<TT>>>().To<Dependency<TTEquatable3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTEquatable4`1</summary><blockquote>

Represents the generic type arguments marker for _IEquatable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTEquatable4<TT>>>().To<Dependency<TTEquatable4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTList`1</summary><blockquote>

Represents the generic type arguments marker for _IList`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTList<TT>>>().To<Dependency<TTList<TT>>>();
            
```


</blockquote></details>


<details><summary>TTList1`1</summary><blockquote>

Represents the generic type arguments marker for _IList`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTList1<TT>>>().To<Dependency<TTList1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTList2`1</summary><blockquote>

Represents the generic type arguments marker for _IList`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTList2<TT>>>().To<Dependency<TTList2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTList3`1</summary><blockquote>

Represents the generic type arguments marker for _IList`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTList3<TT>>>().To<Dependency<TTList3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTList4`1</summary><blockquote>

Represents the generic type arguments marker for _IList`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTList4<TT>>>().To<Dependency<TTList4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObservable`1</summary><blockquote>

Represents the generic type arguments marker for _IObservable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObservable<TT>>>().To<Dependency<TTObservable<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObservable1`1</summary><blockquote>

Represents the generic type arguments marker for _IObservable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObservable1<TT>>>().To<Dependency<TTObservable1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObservable2`1</summary><blockquote>

Represents the generic type arguments marker for _IObservable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObservable2<TT>>>().To<Dependency<TTObservable2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObservable3`1</summary><blockquote>

Represents the generic type arguments marker for _IObservable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObservable3<TT>>>().To<Dependency<TTObservable3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObservable4`1</summary><blockquote>

Represents the generic type arguments marker for _IObservable`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObservable4<TT>>>().To<Dependency<TTObservable4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObserver`1</summary><blockquote>

Represents the generic type arguments marker for _IObserver`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObserver<TT>>>().To<Dependency<TTObserver<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObserver1`1</summary><blockquote>

Represents the generic type arguments marker for _IObserver`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObserver1<TT>>>().To<Dependency<TTObserver1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObserver2`1</summary><blockquote>

Represents the generic type arguments marker for _IObserver`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObserver2<TT>>>().To<Dependency<TTObserver2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObserver3`1</summary><blockquote>

Represents the generic type arguments marker for _IObserver`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObserver3<TT>>>().To<Dependency<TTObserver3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTObserver4`1</summary><blockquote>

Represents the generic type arguments marker for _IObserver`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTObserver4<TT>>>().To<Dependency<TTObserver4<TT>>>();
            
```


</blockquote></details>


<details><summary>TTReadOnlyCollection`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyCollection`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyCollection1`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyCollection`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyCollection2`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyCollection`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyCollection3`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyCollection`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyCollection4`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyCollection`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyList`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyList`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyList1`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyList`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyList2`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyList`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyList3`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyList`1_.
            
</blockquote></details>


<details><summary>TTReadOnlyList4`1</summary><blockquote>

Represents the generic type arguments marker for _IReadOnlyList`1_.
            
</blockquote></details>


<details><summary>TTS</summary><blockquote>

Represents the generic type arguments marker for a value type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTS>>().To<Dependency<TTS>>();
            
```


</blockquote></details>


<details><summary>TTS1</summary><blockquote>

Represents the generic type arguments marker for a value type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTS1>>().To<Dependency<TTS1>>();
            
```


</blockquote></details>


<details><summary>TTS2</summary><blockquote>

Represents the generic type arguments marker for a value type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTS2>>().To<Dependency<TTS2>>();
            
```


</blockquote></details>


<details><summary>TTS3</summary><blockquote>

Represents the generic type arguments marker for a value type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTS3>>().To<Dependency<TTS3>>();
            
```


</blockquote></details>


<details><summary>TTS4</summary><blockquote>

Represents the generic type arguments marker for a value type.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTS4>>().To<Dependency<TTS4>>();
            
```


</blockquote></details>


<details><summary>TTSet`1</summary><blockquote>

Represents the generic type arguments marker for _ISet`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTSet<TT>>>().To<Dependency<TTSet<TT>>>();
            
```


</blockquote></details>


<details><summary>TTSet1`1</summary><blockquote>

Represents the generic type arguments marker for _ISet`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTSet1<TT>>>().To<Dependency<TTSet1<TT>>>();
            
```


</blockquote></details>


<details><summary>TTSet2`1</summary><blockquote>

Represents the generic type arguments marker for _ISet`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTSet2<TT>>>().To<Dependency<TTSet2<TT>>>();
            
```


</blockquote></details>


<details><summary>TTSet3`1</summary><blockquote>

Represents the generic type arguments marker for _ISet`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTSet3<TT>>>().To<Dependency<TTSet3<TT>>>();
            
```


</blockquote></details>


<details><summary>TTSet4`1</summary><blockquote>

Represents the generic type arguments marker for _ISet`1_.
            
```c#

DI.Setup("Composition")
                .Bind<IDependency<TTSet4<TT>>>().To<Dependency<TTSet4<TT>>>();
            
```


</blockquote></details>


<details><summary>TypeAttribute</summary><blockquote>

The injection type can be defined manually using the  `Type`  attribute. This attribute explicitly overrides an injected type, otherwise it would be determined automatically based on the type of the constructor/method, property, or field parameter.
             This attribute is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.
             
```c#

interface IDependency { }
             
            
             class AbcDependency: IDependency { }
            
            
             class XyzDependency: IDependency { }
            
            
             interface IService
             {
                 IDependency Dependency1 { get; }
            
                 IDependency Dependency2 { get; }
             }
            
            
             class Service: IService
             {
                 public Service(
                     [Type(typeof(AbcDependency))] IDependency dependency1,
                     [Type(typeof(XyzDependency))] IDependency dependency2)
                 {
                     Dependency1 = dependency1;
                     Dependency2 = dependency2;
                 }
            
            
                 public IDependency Dependency1 { get; }
            
            
                 public IDependency Dependency2 { get; }
             }
            
            
             DI.Setup("Composition")
                 .Bind<IService>().To<Service>().Root<IService>("Root");
             
```


See also _DependencyAttribute_.

See also _TagAttribute_.

See also _OrdinalAttribute_.

<details><summary>Constructor TypeAttribute(System.Type)</summary><blockquote>

Creates an attribute instance.
            
 - parameter _type_ - The injection type. See also _Bind``1(System.Object[])_ and _Bind``1(System.Object[])_.

</blockquote></details>


</blockquote></details>


</blockquote></details>

