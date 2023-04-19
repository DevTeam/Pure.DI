# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon"/>](http://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

## Key features

Pure.DI is __NOT__ a framework or library, but a source code generator. It generates a partial class for creating object graphs in a pure DI paradigm. To make this object graph accurate, it uses a set of hints that are checked at compile time. Since all the work is done at compile time, at run time you only have efficient code that is ready to be used. This resulting generated code does not depend on library calls or .NET reflection and is efficient in terms of performance and memory consumption since, like normal code, it is subject to optimization and is inserted into the application seamlessly, excluding delegates, boxing, etc..

- [X] DI without any IoC/DI containers, frameworks, dependencies and therefore without any performance impact and side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It generates simple code as well as if you were doing it yourself: de facto just a bunch of nested constructors` calls. And you can see this code at any time.
- [X] A predictable and verified dependency graph is built and verified on the fly while you write your code.
  >All the logic for analyzing the graph of objects, constructors, methods happens at compile time. Thus, the _Pure.DI_ tool notifies the developer about missing or circular dependency, for cases when some dependencies are not suitable for injection, etc., at compile-time. Developers have no chance of getting a program that crashes at runtime due to these errors. All this magic happens simultaneously as the code is written, this way, you have instant feedback between the fact that you made some changes to your code and your code was already checked and ready for use.
- [X] Does not add any dependencies to other assemblies.
  >Using a pure DI approach, you don't add runtime dependencies to your assemblies.
- [X] High performance, including C# and JIT compilers optimizations.
  >All generated code runs as fast as your own, in pure DI style, including compile-time and run-time optimizations. As mentioned above, graph analysis doing at compile-time, but at run-time, there are just a bunch of nested constructors, and that's it.
- [X] Works everywhere.
  >Since a pure DI approach does not use any dependencies or the [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent your code from working as expected on any platform: Full .NET Framework 2.0+, .NET Core, .NET, UWP/XBOX, .NET IoT, Xamarin, Native AOT, etc.
- [X] Ease of use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And it was a deliberate decision: the main reason is that programmers do not need to learn a new API.
- [X] Ultra-fine tuning of generic types.
  >_Pure.DI_ offers special type markers instead of using open generic types. This allows you to more accurately build the object graph and take full advantage of generic types.
- [X] Supports basic .NET BCL types out of the box.
  >_Pure.DI_ already [supports](#base-class-library) many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like Array, IEnumerable, IList, ISet, Func, ThreadLocal, etc. without any extra effort.
- [X] Well suited for creating libraries or frameworks, as well as where resource consumption is especially critical.
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

### Here is our implementation

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

It is important to note that our abstraction and implementation do not know anything about DI magic or any frameworks. Also, please make attention that an instance of type *__Lazy<>__* was used here only as a sample. Still, using this type with nontrivial logic as a dependency is not recommended, so consider replacing it with some simple abstract type.

### Let's glue all together

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
internal partial class Composition
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
dotnet run
```

</details>

## Examples

### Basics
- [Composition root](readme/Examples.md#composition-root)
- [Resolve methods](readme/Examples.md#resolve-methods)
- [Factory](readme/Examples.md#factory)
- [Injection](readme/Examples.md#injection)
- [Generics](readme/Examples.md#generics)
- [Arguments](readme/Examples.md#arguments)
- [Tags](readme/Examples.md#tags)
- [Auto-bindings](readme/Examples.md#auto-bindings)
- [Child composition](readme/Examples.md#child-composition)
- [Multi-contract bindings](readme/Examples.md#multi-contract-bindings)
- [Field Injection](readme/Examples.md#field-injection)
- [Property Injection](readme/Examples.md#property-injection)
- [Complex Generics](readme/Examples.md#complex-generics)
### Lifetimes
- [Singleton](readme/Examples.md#singleton)
- [PerResolve](readme/Examples.md#perresolve)
- [Transient](readme/Examples.md#transient)
- [Disposable Singleton](readme/Examples.md#disposable-singleton)
- [Scope](readme/Examples.md#scope)
- [Default lifetime](readme/Examples.md#default-lifetime)
### Attributes
- [Constructor Ordinal Attribute](readme/Examples.md#constructor-ordinal-attribute)
- [Member Ordinal Attribute](readme/Examples.md#member-ordinal-attribute)
### Base Class Library
- [Func](readme/Examples.md#func)
- [IEnumerable](readme/Examples.md#ienumerable)
- [Array](readme/Examples.md#array)
- [Lazy](readme/Examples.md#lazy)
- [Span and ReadOnlySpan](readme/Examples.md#span-and-readonlyspan)
- [Tuple](readme/Examples.md#tuple)
### Interception
- [Decorator](readme/Examples.md#decorator)
- [Interception](readme/Examples.md#interception)
- [Advanced interception](readme/Examples.md#advanced-interception)
### Hints
- [Resolve Hint](readme/Examples.md#resolve-hint)
- [ThreadSafe Hint](readme/Examples.md#threadsafe-hint)
- [OnDependencyInjection Hint](readme/Examples.md#ondependencyinjection-hint)
- [OnCannotResolve Hint](readme/Examples.md#oncannotresolve-hint)
- [OnInstanceCreation Hint](readme/Examples.md#oninstancecreation-hint)
- [ToString Hint](readme/Examples.md#tostring-hint)
## Composition class

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

Provides a mechanism for releasing unmanaged resources. This method is only generated if the composition contains at least one singleton object that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, call the composition `Dispose()` method:

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
| [OnInstanceCreation](#OnInstanceCreation-Hint)                                                                                     | _On_ or _Off_      | _Off_     | 9.0        |
| [OnInstanceCreationImplementationTypeNameRegularExpression](#OnInstanceCreationImplementationTypeNameRegularExpression-Hint)       | Regular expression | .+        |            |
| [OnInstanceCreationTagRegularExpression](#OnInstanceCreationTagRegularExpression-Hint)                                             | Regular expression | .+        |            |
| [OnInstanceCreationLifetimeRegularExpression](#OnInstanceCreationLifetimeRegularExpression-Hint)                                   | Regular expression | .+        |            |
| [OnDependencyInjection](#OnDependencyInjection-Hint)                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression | .+        |            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#OnDependencyInjectionContractTypeNameRegularExpression-Hint)             | Regular expression | .+        |            |
| [OnDependencyInjectionTagRegularExpression](#OnDependencyInjectionTagRegularExpression-Hint)                                       | Regular expression | .+        |            |
| [OnDependencyInjectionLifetimeRegularExpression](#OnDependencyInjectionLifetimeRegularExpression-Hint)                             | Regular expression | .+        |            |
| [OnCannotResolve](#OnCannotResolve-Hint)                                                                                           | _On_ or _Off_      | _Off_     | 9.0        |
| [OnCannotResolveContractTypeNameRegularExpression](#OnCannotResolveContractTypeNameRegularExpression-Hint)                         | Regular expression | .+        |            |
| [OnCannotResolveTagRegularExpression](#OnCannotResolveTagRegularExpression-Hint)                                                   | Regular expression | .+        |            |
| [OnCannotResolveLifetimeRegularExpression](#OnCannotResolveLifetimeRegularExpression-Hint)                                         | Regular expression | .+        |            |
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

### OnInstanceCreation Hint

Determines whether to generate partial _OnInstanceCreation_ method. This partial method is not generated by default. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnInstanceCreation<T>(ref T value, object? tag, object lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance of type `T`, where `T` is actually type of created instance. To minimize the performance penalty when calling _OnInstanceCreation_, use the three related hints below.

### OnInstanceCreationImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of types for which the method _OnInstanceCreation_ will be called.

### OnInstanceCreationTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnInstanceCreation_ will be called.

### OnInstanceCreationLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnInstanceCreation_ will be called.

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

This hint determines whether object composition will be created in a thread-safe manner. This hint is _On_ by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.

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
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0044 ns</td><td>0.0068 ns</td><td>0.0100 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.3776 ns</td><td>0.1252 ns</td><td>0.2226 ns</td><td>3.2929 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>4.4854 ns</td><td>0.1452 ns</td><td>0.4118 ns</td><td>4.3006 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>7.0763 ns</td><td>0.1496 ns</td><td>0.1249 ns</td><td>7.0460 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>11.1969 ns</td><td>0.2515 ns</td><td>0.6073 ns</td><td>10.9401 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>17.8456 ns</td><td>0.3844 ns</td><td>0.9060 ns</td><td>17.6295 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>21.0332 ns</td><td>0.4494 ns</td><td>1.0594 ns</td><td>20.7954 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>23.9191 ns</td><td>0.5034 ns</td><td>0.7687 ns</td><td>23.6052 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>7,403.1694 ns</td><td>75.8546 ns</td><td>63.3421 ns</td><td>7,403.8330 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                Method</th><th>   Mean</th><th>Error</th><th>StdDev</th><th> Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0204 ns</td><td>0.0233 ns</td><td>0.0334 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.5315 ns</td><td>0.1535 ns</td><td>0.4429 ns</td><td>3.3817 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>4.6739 ns</td><td>0.1621 ns</td><td>0.4755 ns</td><td>4.5309 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>7.6031 ns</td><td>0.2085 ns</td><td>0.5672 ns</td><td>7.4964 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>18.3447 ns</td><td>0.3970 ns</td><td>0.7057 ns</td><td>17.9864 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>21.8540 ns</td><td>0.4615 ns</td><td>0.7047 ns</td><td>21.5849 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>22.5521 ns</td><td>0.4796 ns</td><td>0.8400 ns</td><td>22.2849 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>29.9004 ns</td><td>0.3955 ns</td><td>0.3506 ns</td><td>29.8662 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>6,099.4316 ns</td><td>115.2830 ns</td><td>295.5144 ns</td><td>6,022.7119 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>70.54 ns</td><td>1.152 ns</td><td>1.183 ns</td><td>70.87 ns</td><td>0.90</td><td>0.05</td>
</tr><tr><td>Pure.DI</td><td>76.32 ns</td><td>1.713 ns</td><td>5.024 ns</td><td>73.97 ns</td><td>1.01</td><td>0.08</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>76.88 ns</td><td>1.557 ns</td><td>3.701 ns</td><td>75.23 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>90.67 ns</td><td>4.649 ns</td><td>13.561 ns</td><td>88.75 ns</td><td>1.23</td><td>0.18</td>
</tr><tr><td>DryIoc</td><td>91.71 ns</td><td>1.401 ns</td><td>2.053 ns</td><td>91.13 ns</td><td>1.19</td><td>0.06</td>
</tr><tr><td>LightInject</td><td>410.44 ns</td><td>10.895 ns</td><td>30.731 ns</td><td>400.49 ns</td><td>5.42</td><td>0.53</td>
</tr><tr><td>Autofac</td><td>7,392.18 ns</td><td>186.910 ns</td><td>542.261 ns</td><td>7,226.87 ns</td><td>95.43</td><td>8.62</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>83.25 ns</td><td>3.199 ns</td><td>9.128 ns</td><td>80.64 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>Pure.DI</td><td>86.42 ns</td><td>2.273 ns</td><td>6.701 ns</td><td>85.16 ns</td><td>1.05</td><td>0.14</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>92.44 ns</td><td>2.890 ns</td><td>8.523 ns</td><td>91.41 ns</td><td>1.12</td><td>0.17</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>92.67 ns</td><td>3.598 ns</td><td>10.552 ns</td><td>92.37 ns</td><td>1.12</td><td>0.15</td>
</tr><tr><td>LightInject</td><td>93.13 ns</td><td>3.298 ns</td><td>9.357 ns</td><td>92.00 ns</td><td>1.13</td><td>0.17</td>
</tr><tr><td>DryIoc</td><td>100.80 ns</td><td>3.600 ns</td><td>10.614 ns</td><td>102.84 ns</td><td>1.23</td><td>0.20</td>
</tr><tr><td>Autofac</td><td>10,095.91 ns</td><td>241.064 ns</td><td>679.926 ns</td><td>9,899.44 ns</td><td>122.63</td><td>16.00</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>180.3 ns</td><td>3.97 ns</td><td>11.70 ns</td><td>176.1 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>Pure.DI</td><td>192.8 ns</td><td>4.23 ns</td><td>12.41 ns</td><td>186.9 ns</td><td>1.07</td><td>0.09</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>196.1 ns</td><td>3.88 ns</td><td>8.01 ns</td><td>193.4 ns</td><td>1.08</td><td>0.08</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>199.2 ns</td><td>3.63 ns</td><td>4.59 ns</td><td>197.7 ns</td><td>1.07</td><td>0.06</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>207.6 ns</td><td>4.10 ns</td><td>3.63 ns</td><td>207.5 ns</td><td>1.12</td><td>0.06</td>
</tr><tr><td>LightInject</td><td>209.7 ns</td><td>4.94 ns</td><td>14.40 ns</td><td>203.8 ns</td><td>1.17</td><td>0.10</td>
</tr><tr><td>DryIoc</td><td>224.0 ns</td><td>6.84 ns</td><td>19.73 ns</td><td>218.6 ns</td><td>1.24</td><td>0.12</td>
</tr><tr><td>Autofac</td><td>10,355.1 ns</td><td>206.58 ns</td><td>486.94 ns</td><td>10,186.9 ns</td><td>57.61</td><td>4.24</td>
</tr></tbody></table>

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.2846/22H2/2022Update)
Intel Core i7-10850H CPU 2.70GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
</code></pre>

</details>
