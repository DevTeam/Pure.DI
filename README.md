# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon"/>](http://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

## Key features

Pure.DI is not a framework or library, but a source code generator. It generates a partial class to create object graphs in the Pure DI paradigm. To make this object graph accurate, it uses a set of hints that are checked at compile time. Since all the work is done at compile time, you only have effective code ready to use at run time. The generated code is independent of .NET library calls or reflection and is efficient in terms of performance and memory consumption because, like normal code, it is subject to all optimizations and integrates seamlessly into any application - without using delegates, extra method calls, boxing, type conversions, etc.

- [X] DI without any IoC/DI containers, frameworks, dependencies and therefore without performance impact or side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It generates simple code as well as if you were doing it yourself: de facto just a bunch of nested constructors` calls. And you can see this code at any time.
- [X] A predictable and verifiable dependency graph is built and verified on the fly while you write the code.
  >All the logic for analyzing the graph of objects, constructors, methods happens at compile time. Thus, the _Pure.DI_ tool notifies the developer about missing or circular dependency, for cases when some dependencies are not suitable for injection, etc., at compile-time. Developers have no chance of getting a program that crashes at runtime due to these errors. All this magic happens simultaneously as the code is written, this way, you have instant feedback between the fact that you made some changes to your code and your code was already checked and ready for use.
- [X] Does not add any dependencies to other assemblies.
  >Using a pure DI approach, you don't add runtime dependencies to your assemblies.
- [X] Highest performance, including compiler and JIT optimizations.
  >All generated code runs as fast as your own, in pure DI style, including compile-time and run-time optimizations. As mentioned above, graph analysis doing at compile-time, but at run-time, there are just a bunch of nested constructors, and that's it.
- [X] It works everywhere.
  >Since a pure DI approach does not use any dependencies or the [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent your code from working as expected on any platform: Full .NET Framework 2.0+, .NET Core, .NET, UWP/XBOX, .NET IoT, Xamarin, Native AOT, etc.
- [X] Ease of use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And it was a deliberate decision: the main reason is that programmers do not need to learn a new API.
- [X] Ultra-fine tuning of generic types.
  >_Pure.DI_ offers special type markers instead of using open generic types. This allows you to more accurately build the object graph and take full advantage of generic types.
- [X] Supports basic .NET BCL types out of the box.
  >_Pure.DI_ already [supports](#base-class-library) many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like `Array`, `IEnumerable<T>`, `IList<T>`, `ISet<T>`, `Func<T>`, `ThreadLocal`, `Task<T>`, `MemoryPool<T>`, `ArrayPool<T>`, `ReadOnlyMemory<T>`, `Memory<T>`, `ReadOnlySpan<T>`, `Span<T>`, `IComparer<T>`, `IEqualityComparer<T>` and etc. without any extra effort.
- [X] Good for creating libraries or frameworks and where resource consumption is particularly critical.
  >High performance, zero memory consumption/preparation overhead and no dependencies make it an ideal assistant for building libraries and frameworks.

## Schrödinger's cat shows how it works [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](samples/ShroedingersCat)

### The reality is that

![Cat](readme/cat.png?raw=true)

### Let's create an abstraction

```c#
interface IBox<out T> { T Content { get; } }

interface ICat { State State { get; } }

enum State { Alive, Dead }
```

### Here's our implementation

```c#
class CardboardBox<T> : IBox<T>
{
    public CardboardBox(T content) => Content = content;

    public T Content { get; }
}

class ShroedingersCat : ICat
{
  // Represents the superposition of the states
  private readonly Lazy<State> _superposition;

  public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;

  // Decoherence of the superposition at the time of observation via an irreversible process
  public State State => _superposition.Value;

  public override string ToString() => $"{State} cat";
}
```

It is important to note that our abstraction and implementation knows nothing about DI magic or any frameworks. Also, please note that an instance of type *__Lazy<>__* has been used here only as an example. However, using this type with non-trivial logic as a dependency is not recommended, so consider replacing it with some simple abstract type.

### Let's glue it all together

Add a package reference to

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)

Package Manager

```shell
Install-Package Pure.DI
```

.NET CLI
  
```shell
dotnet add package Pure.DI
```

Bind abstractions to their implementations or factories, define lifetimes and other options in a class like the following:

```c#
partial class Composition
{
  // In fact, this code is never run, and the method can have any name or be a constructor, for example,
  // and can be in any part of the compiled code because this is just a hint to set up an object graph.
  // Here the setup is part of the generated class, just as an example.
  private static void Setup() => DI.Setup(nameof(Composition))
      // Models a random subatomic event that may or may not occur
      .Bind<Random>().As(Singleton).To<Random>()
      // Represents a quantum superposition of 2 states: Alive or Dead
      .Bind<State>().To(ctx =>
      {
          ctx.Inject<Random>(out var random);
          return (State)random.Next(2);
      })
      // Represents schrodinger's cat
      .Bind<ICat>().To<ShroedingersCat>()
      // Represents a cardboard box with any content
      .Bind<IBox<TT>>().To<CardboardBox<TT>>()
      // Composition Root
      .Root<Program>("Root");
}
```

The code above is just a chain of hints to define the dependency graph used to create a *__Composition__* class with a *__Root__* property that creates the *__Program__* composition root below. It doesn't really make sense to run this code because it doesn't do anything at runtime. So it can be placed anywhere in the class (in methods, constructors or properties) and preferably where it will not be called. Its purpose is only to check the dependency syntax and help build the dependency graph at compile time to create the *__Composition__* class. The first argument to the _Setup_ method specifies the name of the class to be generated.

### Time to open boxes!

```c#
class Program
{
  // Composition Root, a single place in an application
  // where the composition of the object graphs for an application take place
  public static void Main() => new Composition().Root.Run();

  private readonly IBox<ICat> _box;

  internal Program(IBox<ICat> box) => _box = box;

  private void Run() => Console.WriteLine(_box);
}
```

*__Root__* is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) here, a single place in an application where the composition of the object graphs for an application takes place. Each instance is resolved by a strongly-typed block of statements like the operator [*__new__*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator), which are compiling with all optimizations with minimal impact on performance or memory consumption. The generated _Composition_ class contains a _Root_ property that allows you to resolve an instance of the _Program_ type.

<details>
<summary>Root property</summary>

```c#
public Sample.Program Root
{
  get
  {
    Func<State> stateFunc = new Func<State>(() =>
    {
      if (_randomSingleton == null)
      {
        lock (_lockObject)
        {
          if (_randomSingleton == null)
          {
            _randomSingleton = new Random();
          }
        }
      }
      
      return (State)_randomSingleton.Next(2);      
    });
    
    return new Program(
      new CardboardBox<ICat>(
        new ShroedingersCat(
          new Lazy<Sample.State>(
            stateFunc))));    
  }
}
```

</details>

You can find a complete analogue of this application with top level statements [here](Samples/ShroedingersCatTopLevelStatements). For a top level statements application the name of generated composer is "Composer" by default if it was not override in the Setup call.

_Pure.DI_ works the same as calling a set of nested constructors, but allows dependency injection. And that's a reason to take full advantage of Dependency Injection everywhere, every time, without any compromise!

<details>
<summary>Just try!</summary>

Download the example project code

```shell
git clone https://github.com/DevTeam/Pure.DI.Example.git
```

And run it from solution root folder

```shell
cd ./Pure.DI.Example
```

```shell
dotnet run
```

</details>

## Examples

### Basics
- [Composition root](readme/composition-root.md)
- [Resolve methods](readme/resolve-methods.md)
- [Factory](readme/factory.md)
- [Injection](readme/injection.md)
- [Generics](readme/generics.md)
- [Arguments](readme/arguments.md)
- [Tags](readme/tags.md)
- [Auto-bindings](readme/auto-bindings.md)
- [Child composition](readme/child-composition.md)
- [Multi-contract bindings](readme/multi-contract-bindings.md)
- [Field injection](readme/field-injection.md)
- [Method injection](readme/method-injection.md)
- [Property injection](readme/property-injection.md)
- [Complex generics](readme/complex-generics.md)
- [Partial class](readme/partial-class.md)
- [Dependent compositions](readme/dependent-compositions.md)
- [Required properties or fields](readme/required-properties-or-fields.md)
### Lifetimes
- [Singleton](readme/singleton.md)
- [PerResolve](readme/perresolve.md)
- [Transient](readme/transient.md)
- [Disposable singleton](readme/disposable-singleton.md)
- [Scope](readme/scope.md)
- [Default lifetime](readme/default-lifetime.md)
### Attributes
- [Constructor ordinal attribute](readme/constructor-ordinal-attribute.md)
- [Member ordinal attribute](readme/member-ordinal-attribute.md)
- [Tag attribute](readme/tag-attribute.md)
- [Type attribute](readme/type-attribute.md)
- [Custom attributes](readme/custom-attributes.md)
### Base Class Library
- [Func](readme/func.md)
- [Enumerable](readme/enumerable.md)
- [Array](readme/array.md)
- [Lazy](readme/lazy.md)
- [Span and ReadOnlySpan](readme/span-and-readonlyspan.md)
- [Tuple](readme/tuple.md)
- [Service provider](readme/service-provider.md)
- [Service collection](readme/service-collection.md)
- [Func with arguments](readme/func-with-arguments.md)
- [Overriding the BCL binding](readme/overriding-the-bcl-binding.md)
### Interception
- [Decorator](readme/decorator.md)
- [Interception](readme/interception.md)
- [Advanced interception](readme/advanced-interception.md)
### Hints
- [Resolve hint](readme/resolve-hint.md)
- [ThreadSafe hint](readme/threadsafe-hint.md)
- [OnDependencyInjection hint](readme/ondependencyinjection-hint.md)
- [OnCannotResolve hint](readme/oncannotresolve-hint.md)
- [OnNewInstance hint](readme/onnewinstance-hint.md)
- [ToString hint](readme/tostring-hint.md)
### Applications
- [Console application](readme/ConsoleTemplate.md)
- [WPF application](readme/WpfTemplate.md)
- [Top level statements console application](readme/ConsoleTopLevelStatementsTemplate.md)

## Generated Code

Each generated class, hereinafter referred to as _composition_, needs to be configured. It starts with the `Setup(...)` method:

```c#
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

<details>
<summary>The following class will be generated</summary>

```c#
partial class Composition
{
    public Composition() { }

    internal Composition(Composition parent) { }

    public IService Root
    {
        get
        {
            return new Service(new Dependency());
        }
    }

    public T Resolve<T>()  { ... }

    public T Resolve<T>(object? tag)  { ... }

    public object Resolve(System.Type type) { ... }

    public object Resolve(System.Type type, object? tag) { ... }
}
```

</details>

<details>
<summary>Setup arguments</summary>

The first parameter is used to specify the name of the composition class. All setups with the same name will be combined to create one composition class. In addition, this name may contain a namespace, for example for `Sample.Composition` the composition class is generated:

```c#
namespace Sample
{
    partial class Composition
    {
        ...
    }
}
```

The second optional parameter can have several values to determine the kind of composition.

### CompositionKind.Public

This is the default value. If this value is specified, a composition class will be created.

### CompositionKind.Internal

If this value is specified, the class will not be generated, but this setup can be used for others as a base. For example:

```c#
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind<IDependency>().To<Dependency>();

DI.Setup("Composition").DependsOn("BaseComposition")
    .Bind<IService>().To<Service>();    
```

When the composition _CompositionKind.Public_ flag is set in the composition setup, it can also be the base composition for others like in the example above.

### CompositionKind.Global

If this value is specified, no composition class will be created, but this setup is the base for all setups in the current project, and `DependsOn(...)` is not required.

</details>

<details>
<summary>Constructors</summary>

### Default constructor

Everything is quite banal, this constructor simply initializes the internal state.

### Argument constructor

It replaces the default constructor and is only created if at least one argument is provided. For example:

```c#
DI.Setup("Composition")
    .Arg<string>("name")
    .Arg<int>("id")
    ...
```

In this case, the argument constructor looks like this:

```c#
public Composition(string name, int id) { ... }
```

and default constructor is missing.

### Child constructor

This constructor is always available and is used to create a child composition based on the parent composition:

```c#
var parentComposition = new Composition();
var childComposition = new Composition(parentComposition); 
```

The child composition inherits the state of the parent composition in the form of arguments and singleton objects. States are copied, and compositions are completely independent, except when calling the _Dispose()_ method on the parent container before disposing of the child container, because the child container can use singleton objects created before it was created.

</details>

<details>
<summary>Properties</summary>

To be able to quickly and conveniently create an object graph, a set of properties is generated. These properties are called compositions roots here. The type of the property is the type of a root object created by the composition. Accordingly, each access to the property leads to the creation of a composition with the root element of this type.

### Public Composition Roots

To be able to use a specific composition root, that root must be explicitly defined by the _Root_ method with a specific name and type:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

In this case, the property for type _IService_ will be named _MyService_ and will be available for direct use. The result of its use will be the creation of a composition of objects with a root of type _IService_:

```c#
public IService MyService
{
    get
    { 
        ...
        return new Service(...);
    }
}
```

This is [recommended way](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) to create a composition root. A composition class can contain any number of roots.

### Private Composition Roots

When the root name is empty, a private composition root is created. This root is used in these _Resolve_ methods in the same way as public roots. For example:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>();
```

```c#
private IService Root1ABB3D0
{
    get { ... }
}
```

These properties have a random name and a private accessor and cannot be used directly from code. Don't try to use them. Private composition roots can be resolved by the _Resolve_ methods.

</details>

<details>
<summary>Methods</summary>

### Resolve

By default a set of four _Resolve_ methods are generated:

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods can resolve public composition roots as well as private roots and are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach when the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) way to create composition roots. To control the generation of these methods, see the _Resolve_ hint.

### Dispose

Provides a mechanism for releasing unmanaged resources. This method is only generated if the composition contains at least one singleton instance that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, call the composition `Dispose()` method:

```c#
using(var composition = new Composition())
{
    ...
}
```

</details>

<details>
<summary>Setup hints</summary>

## Setup hints

Hints are used to fine-tune code generation. Setup hints can be used as in the following example:

```c#
DI.Setup("Composition")
    .Hint(Hint.Resolve, "Off")
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.ToString, "On")
    ...
```

In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example:

```c#
// Resolve = Off
// ThreadSafe = Off
DI.Setup("Composition")
    .Hint(Hint.ToString, "On")
    ...
```

| Hint                                                                                                                               | Values             | Default   | C# version |
|------------------------------------------------------------------------------------------------------------------------------------|--------------------|-----------|------------|
| [Resolve](#Resolve-Hint)                                                                                                           | _On_ or _Off_      | _On_      |            |
| [OnNewInstance](#OnNewInstance-Hint)                                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnNewInstanceImplementationTypeNameRegularExpression](#OnNewInstanceImplementationTypeNameRegularExpression-Hint)                 | Regular expression | .+        |            |
| [OnNewInstanceTagRegularExpression](#OnNewInstanceTagRegularExpression-Hint)                                                       | Regular expression | .+        |            |
| [OnNewInstanceLifetimeRegularExpression](#OnNewInstanceLifetimeRegularExpression-Hint)                                             | Regular expression | .+        |            |
| [OnDependencyInjection](#OnDependencyInjection-Hint)                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression | .+        |            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#OnDependencyInjectionContractTypeNameRegularExpression-Hint)             | Regular expression | .+        |            |
| [OnDependencyInjectionTagRegularExpression](#OnDependencyInjectionTagRegularExpression-Hint)                                       | Regular expression | .+        |            |
| [OnDependencyInjectionLifetimeRegularExpression](#OnDependencyInjectionLifetimeRegularExpression-Hint)                             | Regular expression | .+        |            |
| [OnCannotResolve](#OnCannotResolve-Hint)                                                                                           | _On_ or _Off_      | _Off_     | 9.0        |
| [OnCannotResolveContractTypeNameRegularExpression](#OnCannotResolveContractTypeNameRegularExpression-Hint)                         | Regular expression | .+        |            |
| [OnCannotResolveTagRegularExpression](#OnCannotResolveTagRegularExpression-Hint)                                                   | Regular expression | .+        |            |
| [OnCannotResolveLifetimeRegularExpression](#OnCannotResolveLifetimeRegularExpression-Hint)                                         | Regular expression | .+        |            |
| [OnNewRoot](#OnNewRoot-Hint)                                                                                                       | _On_ or _Off_      | _Off_     |            |
| [ToString](#ToString-Hint)                                                                                                         | _On_ or _Off_      | _Off_     |            |
| [ThreadSafe](#ThreadSafe-Hint)                                                                                                     | _On_ or _Off_      | _On_      |            |
| [ResolveMethodModifiers](#ResolveMethodModifiers-Hint)                                                                             | Method modifier    | _public_  |            |
| [ResolveMethodName](#ResolveMethodName-Hint)                                                                                       | Method name        | _Resolve_ |            |
| [ResolveByTagMethodModifiers](#ResolveByTagMethodModifiers-Hint)                                                                   | Method modifier    | _public_  |            |
| [ResolveByTagMethodName](#ResolveByTagMethodName-Hint)                                                                             | Method name        | _Resolve_ |            |
| [ObjectResolveMethodModifiers](#ObjectResolveMethodModifiers-Hint)                                                                 | Method modifier    | _public_  |            |
| [ObjectResolveMethodName](#ObjectResolveMethodName-Hint)                                                                           | Method name        | _Resolve_ |            |
| [ObjectResolveByTagMethodModifiers](#ObjectResolveByTagMethodModifiers-Hint)                                                       | Method modifier    | _public_  |            |
| [ObjectResolveByTagMethodName](#ObjectResolveByTagMethodName-Hint)                                                                 | Method name        | _Resolve_ |            |
| [DisposeMethodModifiers](#DisposeMethodModifiers-Hint)                                                                             | Method modifier    | _public_  |            |
| [FormatCode](#FormatCode-Hint)                                                                                                     | _On_ or _Off_      | _Off_     |            |

### Resolve Hint

Determines whether to generate [_Resolve_ methods](#resolve-methods). By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no [private composition roots](#Private-Roots) will be generated in this case. The composition will be tiny and will only have [public roots](#Public-Roots). When the _Resolve_ hint is disabled, only the public root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.

### OnNewInstance Hint

Determines whether to generate partial _OnNewInstance_ method. This partial method is not generated by default. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnNewInstance<T>(ref T value, object? tag, object lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance of type `T`, where `T` is actually type of created instance. To minimize the performance penalty when calling _OnNewInstance_, use the three related hints below.

### OnNewInstanceImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnNewInstance_ is in the _On_ state and you want to limit the set of types for which the method _OnNewInstance_ will be called.

### OnNewInstanceTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnNewInstance_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnNewInstance_ will be called.

### OnNewInstanceLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnNewInstance_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnNewInstance_ will be called.

### OnDependencyInjection Hint

Determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden when generated. This can be useful, for example, for [interception](#Interception).

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize the performance penalty when calling _OnDependencyInjection_, use the three related hints below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnDependencyInjection_ will be called.

### OnCannotResolve Hint

Determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden on creation.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing bindings by mistake, use the two related hints below.

### OnNewRoot Hint

Determines whether to generate a static partial `OnNewRoot<TContract, T>(...)` method to handle the new composition root registration event.

```c#
// OnNewRoot = On
DI.Setup("Composition")
    ...
```

### OnCannotResolveContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnCannotResolve_ will be called.

### OnCannotResolveTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnCannotResolve_ will be called.

### OnCannotResolveLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnCannotResolve_ will be called.

### ToString Hint

Determine if the _ToString()_ method should be generated. This method provides a text-based class diagram in the format [mermaid](https://mermaid.js.org/). To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/).

```c#
// ToString = On
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
    
var composition = new Composition();
string classDiagram = composition.ToString(); 
```

### ThreadSafe Hint

This hint determines whether the composition of objects will be created in a thread-safe manner. The default value of this hint is _On_. It is good practice not to use threads when creating an object graph, in which case the hint can be disabled, resulting in a slight performance gain.

```c#
// ThreadSafe = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

### ResolveMethodModifiers Hint

Overrides modifiers of the method `public T Resolve<T>()`.

### ResolveMethodName Hint

Overrides name of the method `public T Resolve<T>()`.

### ResolveByTagMethodModifiers Hint

Overrides modifiers of the method `public T Resolve<T>(object? tag)`.

### ResolveByTagMethodName Hint

Overrides name of the method `public T Resolve<T>(object? tag)`.

### ObjectResolveMethodModifiers Hint

Overrides modifiers of the method `public object Resolve(Type type)`.

### ObjectResolveMethodName Hint

Overrides name of the method `public object Resolve(Type type)`.

### ObjectResolveByTagMethodModifiers Hint

Overrides modifiers of the method `public object Resolve(Type type, object? tag)`.

### ObjectResolveByTagMethodName Hint

Overrides name of the method `public object Resolve(Type type, object? tag)`.

### DisposeMethodModifiers Hint

Overrides modifiers of the method `public void Dispose()`.

### FormatCode Hint

Specifies whether the generated code should be formatted. This option consumes a lot of CPU resources. This hint can be useful for examining the generated code or for giving presentations, for example.

</details>

## Development environment requirements

- [.NET SDK 6.0.4xx or newer](https://dotnet.microsoft.com/download/dotnet/6.0)
- [C# 8 or newer](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-80)

## Supported frameworks

- [.NET and .NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET Framework](https://docs.microsoft.com/en-us/dotnet/framework/) 2.0+
- [UWP/XBOX](https://docs.microsoft.com/en-us/windows/uwp/index)
- [.NET IoT](https://dotnet.microsoft.com/apps/iot)
- [Xamarin](https://dotnet.microsoft.com/apps/xamarin)
- [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf)
- [.NET Multi-platform App UI (MAUI)](https://docs.microsoft.com/en-us/dotnet/maui/)

## Project template

Install the DI template [Pure.DI.Templates](https://www.nuget.org/packages/Pure.DI.Templates)

```shell
dotnet new -i Pure.DI.Templates
```

Create a "Sample" console application from the template *__di__*

```shell
dotnet new di -o ./Sample
```

And run it

```shell
dotnet run --project Sample
```

Please see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates) for more details about the template.

## Troubleshooting

<details>
<summary>Generated files</summary>

You can set build properties to save the generated file and control where the generated files are stored. In a project file, add the <EmitCompilerGeneratedFiles> element to a <PropertyGroup>, and set its value to true. Build your project again. Now, the generated files are created under obj/Debug/netX.X/generated/Pure.DI/Pure.DI.SourceGenerator. The components of the path map to the build configuration, target framework, source generator project name, and fully qualified type name of the generator. You can choose a more convenient output folder by adding the <CompilerGeneratedFilesOutputPath> element to the application's project file. For example:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
    </PropertyGroup>
    
</Project>
```

</details>

<details>
<summary>Log files</summary>

You can set build properties to save the log file. In the project file, add a <PureDILogFile> element to the <PropertyGroup> and set the path to the log directory, and add the related element `<CompilerVisibleProperty Include="PureDILogFile" />` to the <ItemGroup> to make this property visible in the source generator. To change the log level, specify the same with the _PureDISeverity_ property, as in the example below:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <PureDILogFile>.logs\Pure.DI.log</PureDILogFile>
        <PureDISeverity>Info</PureDISeverity>
    </PropertyGroup>

    <ItemGroup>
        <CompilerVisibleProperty Include="PureDILogFile" />
        <CompilerVisibleProperty Include="PureDISeverity" />
    </ItemGroup>

</Project>
```

The _PureDISeverity_ property has several options available:

| Severity | Description                                                            |
|----------|------------------------------------------------------------------------|
| Hidden   | Debug information.                                                     |
| Info     | Information that does not indicate a problem (i.e. not prescriptive).  |
| Warning  | Something suspicious, but allowed. This is the default value.          |
| Error    | Something not allowed by the rules of the language or other authority. |

</details>


## Benchmarks

<details>
<summary>Transient</summary>

<table>
<thead><tr><th>                Method</th><th>   Mean</th><th>Error</th><th>StdDev</th><th> Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0128 ns</td><td>0.0166 ns</td><td>0.0238 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.9792 ns</td><td>0.1370 ns</td><td>0.3998 ns</td><td>3.8511 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>4.8851 ns</td><td>0.1555 ns</td><td>0.2375 ns</td><td>4.8104 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>7.9274 ns</td><td>0.2154 ns</td><td>0.5364 ns</td><td>7.7121 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>11.0228 ns</td><td>0.2509 ns</td><td>0.6249 ns</td><td>10.8849 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>20.5335 ns</td><td>0.4306 ns</td><td>0.6954 ns</td><td>20.4471 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>22.7638 ns</td><td>0.4747 ns</td><td>0.8799 ns</td><td>22.6135 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>23.4549 ns</td><td>0.4783 ns</td><td>0.9328 ns</td><td>23.4470 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>8,924.6020 ns</td><td>178.0725 ns</td><td>277.2375 ns</td><td>8,920.4597 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                Method</th><th>   Mean</th><th>Error</th><th>StdDev</th><th> Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0175 ns</td><td>0.0208 ns</td><td>0.0223 ns</td><td>0.0025 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>4.4046 ns</td><td>0.1675 ns</td><td>0.4913 ns</td><td>4.2524 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>5.4675 ns</td><td>0.1651 ns</td><td>0.4867 ns</td><td>5.2425 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>7.8375 ns</td><td>0.2136 ns</td><td>0.3262 ns</td><td>7.7274 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>19.3453 ns</td><td>0.4133 ns</td><td>0.7964 ns</td><td>19.0213 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>21.9625 ns</td><td>0.4665 ns</td><td>0.5900 ns</td><td>21.9175 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>23.4351 ns</td><td>0.3397 ns</td><td>0.2837 ns</td><td>23.3851 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>31.4078 ns</td><td>0.6519 ns</td><td>1.0150 ns</td><td>30.9192 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>6,292.0519 ns</td><td>123.0989 ns</td><td>184.2486 ns</td><td>6,275.1659 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>79.40 ns</td><td>2.333 ns</td><td>6.842 ns</td><td>76.23 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>83.99 ns</td><td>2.211 ns</td><td>6.483 ns</td><td>80.88 ns</td><td>1.07</td><td>0.12</td>
</tr><tr><td>Pure.DI</td><td>84.28 ns</td><td>2.507 ns</td><td>7.314 ns</td><td>81.52 ns</td><td>1.07</td><td>0.11</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>91.09 ns</td><td>3.432 ns</td><td>10.011 ns</td><td>88.74 ns</td><td>1.16</td><td>0.17</td>
</tr><tr><td>DryIoc</td><td>109.17 ns</td><td>3.266 ns</td><td>9.579 ns</td><td>106.92 ns</td><td>1.38</td><td>0.16</td>
</tr><tr><td>LightInject</td><td>441.57 ns</td><td>8.785 ns</td><td>21.877 ns</td><td>431.23 ns</td><td>5.56</td><td>0.58</td>
</tr><tr><td>Autofac</td><td>8,645.17 ns</td><td>170.212 ns</td><td>319.699 ns</td><td>8,604.62 ns</td><td>109.90</td><td>10.55</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>79.99 ns</td><td>2.698 ns</td><td>7.954 ns</td><td>76.70 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>88.79 ns</td><td>2.717 ns</td><td>7.884 ns</td><td>86.02 ns</td><td>1.12</td><td>0.15</td>
</tr><tr><td>Pure.DI</td><td>92.69 ns</td><td>3.242 ns</td><td>9.509 ns</td><td>88.42 ns</td><td>1.17</td><td>0.15</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>93.28 ns</td><td>2.674 ns</td><td>7.884 ns</td><td>90.48 ns</td><td>1.18</td><td>0.14</td>
</tr><tr><td>LightInject</td><td>99.37 ns</td><td>3.547 ns</td><td>10.347 ns</td><td>97.70 ns</td><td>1.25</td><td>0.18</td>
</tr><tr><td>DryIoc</td><td>108.40 ns</td><td>3.156 ns</td><td>9.257 ns</td><td>104.45 ns</td><td>1.37</td><td>0.17</td>
</tr><tr><td>Autofac</td><td>10,368.08 ns</td><td>203.428 ns</td><td>429.098 ns</td><td>10,342.65 ns</td><td>130.12</td><td>15.68</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                Method</th><th>Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>188.5 ns</td><td>3.90 ns</td><td>11.51 ns</td><td>183.6 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>Pure.DI</td><td>188.7 ns</td><td>3.82 ns</td><td>10.12 ns</td><td>184.9 ns</td><td>1.00</td><td>0.08</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>199.6 ns</td><td>4.25 ns</td><td>12.52 ns</td><td>195.1 ns</td><td>1.06</td><td>0.09</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>207.1 ns</td><td>4.17 ns</td><td>11.06 ns</td><td>203.2 ns</td><td>1.10</td><td>0.07</td>
</tr><tr><td>LightInject</td><td>213.0 ns</td><td>4.65 ns</td><td>13.71 ns</td><td>206.0 ns</td><td>1.13</td><td>0.10</td>
</tr><tr><td>DryIoc</td><td>222.9 ns</td><td>4.56 ns</td><td>13.38 ns</td><td>217.4 ns</td><td>1.19</td><td>0.11</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>234.3 ns</td><td>5.20 ns</td><td>15.17 ns</td><td>228.7 ns</td><td>1.25</td><td>0.10</td>
</tr><tr><td>Autofac</td><td>9,865.7 ns</td><td>196.14 ns</td><td>240.87 ns</td><td>9,755.9 ns</td><td>51.19</td><td>4.06</td>
</tr></tbody></table>

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet v0.13.6, Windows 10 (10.0.19045.3208/22H2/2022Update)
Intel Core i7-10850H CPU 2.70GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2
</code></pre>

</details>
