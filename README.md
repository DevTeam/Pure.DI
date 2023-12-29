# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon)](https://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

## Key features

Pure.DI is not a framework or library, but a source code generator for creating object graphs. To make them accurate, the developer uses a set of intuitive hints from the Pure.DI API. During the compilation phase, Pure.DI determines the optimal graph structure, checks its correctness, and generates partial class code to create object graphs in the Pure DI paradigm using only basic language constructs. The resulting generated code is robust, works everywhere, throws no exceptions, does not depend on .NET library calls or .NET reflections, is efficient in terms of performance and memory consumption, and is subject to all optimizations. This code can be easily integrated into an application because it does not use unnecessary delegates, additional calls to any methods, type conversions, boxing/unboxing, etc.

- [X] DI without any IoC/DI containers, frameworks, dependencies and hence no performance impact or side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It uses basic language constructs to create simple code as well as if you were doing it yourself: de facto it's just a bunch of nested constructor calls. This code can be viewed, analyzed at any time, and debugged.
- [X] A predictable and verified dependency graph is built and validated on the fly while writing code.
  >All logic for analyzing the graph of objects, constructors and methods takes place at compile time. _Pure.DI_ notifies the developer at compile time about missing or ring dependencies, cases when some dependencies are not suitable for injection, etc. The developer has no chance to get a program that will crash at runtime because of some exception related to incorrect object graph construction. All this magic happens at the same time as the code is written, so you have instant feedback between the fact that you have made changes to your code and the fact that your code is already tested and ready to use.
- [X] Does not add any dependencies to other assemblies.
  >When using pure DI, no dependencies are added to assemblies because only basic language constructs and nothing more are used.
- [X] Highest performance, including compiler and JIT optimization and minimal memory consumption.
  >All generated code runs as fast as your own, in pure DI style, including compile-time and run-time optimization. As mentioned above, graph analysis is done at compile time, and at runtime there are only a bunch of nested constructors, and that's it. Memory is spent only on the object graph being created.
- [X] It works everywhere.
  >Since the pure DI approach does not use any dependencies or [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent the code from running as expected on any platform: Full .NET Framework 2.0+, .NET Core, .NET, UWP/XBOX, .NET IoT, Xamarin, Native AOT, etc.
- [X] Ease of Use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And this was a conscious decision: the main reason is that programmers don't need to learn a new API.
- [X] Superfine customization of generic types.
  >In _Pure.DI_ it is proposed to use special marker types instead of using open generic types. This allows you to build the object graph more accurately and take full advantage of generic types.
- [X] Supports the major .NET BCL types out of the box.
  >_Pure.DI_ already [supports](#base-class-library) many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like `Array`, `IEnumerable<T>`, `IList<T>`, `ISet<T>`, `Func<T>`, `ThreadLocal`, `Task<T>`, `MemoryPool<T>`, `ArrayPool<T>`, `ReadOnlyMemory<T>`, `Memory<T>`, `ReadOnlySpan<T>`, `Span<T>`, `IComparer<T>`, `IEqualityComparer<T>` and etc. without any extra effort.
- [X] Good for building libraries or frameworks where resource consumption is particularly critical.
  >Its high performance, zero memory consumption/preparation overhead, and lack of dependencies make it ideal for building libraries and frameworks.

## Schrödinger's cat will demonstrate how it all works [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](samples/ShroedingersCat)

### The reality is

![Cat](readme/cat.png?raw=true)

### Let's create an abstraction

```c#
interface IBox<out T>
{
    T Content { get; }
}

interface ICat
{
    State State { get; }
}

enum State
{
    Alive,
    Dead
}
```

### Here's our implementation

```c#
class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

```

It is important to note that our abstraction and implementation knows nothing about the magic of DI or any frameworks.

### Let's glue it all together

Add the _Pure.DI_ package to your project:

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

Let's bind the abstractions to their implementations and set up the creation of the object graph:

```c#
partial class Composition
{
  // In fact, this code is never run,
  // and the method can have any name or be a constructor, for example,
  // and can be in any part of the compiled code
  // because this is just a hint to set up an object graph.
  // Here the customization is part of the generated class, just as an example.
  // But in general it can be done anywhere in the code.
  private static void Setup() => 
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind<Random>().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states:
        // Alive or Dead
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

The above code specifies the generation of a partial class named *__Composition__*, this name is defined in the `DI.Setup(nameof(Composition))` call. This class contains a *__Root__* property that returns a graph of objects with an object of type *__Program__* as the root. The type and name of the property is defined by calling `Root<Program>("Root")`. The code of the generated class looks as follows:

```c#
partial class Composition
{
    private object _lockObject = new object();
    private Random _randomSingleton;    
    
    public Program Root
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
    
    public T Resolve<T>()
    {
        ...
    }
    
    public object Resolve(Type type)
    {
        ...
    }    
}
```

The `public Program Root { get; }` property here is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/), the only place in the application where the composition of the object graph for the application takes place. Each instance is created by only basic language constructs, which compiles with all optimizations with minimal impact on performance and memory consumption. In general, applications may have multiple composition roots and thus such properties. Each composition root must have its own unique name, which is defined when the `Root<T>(string name)` method is called, as shown in the above code.

### Time to open boxes!

```c#
class Program(IBox<ICat> box)
{
  // Composition Root, a single place in an application
  // where the composition of the object graphs for an application take place
  static void Main() => new Composition().Root.Run();

  private void Run() => Console.WriteLine(box);
}
```

The full analog of this application with top-level statements can be found [here](samples/ShroedingersCatTopLevelStatements).

## To summarize

_Pure.DI_ creates efficient code in a pure DI paradigm, using only basic language constructs as if you were writing code by hand. This allows you to take full advantage of Dependency Injection everywhere and always, without any compromise!

<details>
<summary>Just try!</summary>

Download a sample project

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
- [Composition roots](readme/composition-roots.md)
- [Resolve methods](readme/resolve-methods.md)
- [Factory](readme/factory.md)
- [Injection](readme/injection.md)
- [Generics](readme/generics.md)
- [Arguments](readme/arguments.md)
- [Root arguments](readme/root-arguments.md)
- [Composition root kinds](readme/composition-root-kinds.md)
- [Tags](readme/tags.md)
- [Auto-bindings](readme/auto-bindings.md)
- [Child composition](readme/child-composition.md)
- [Multi-contract bindings](readme/multi-contract-bindings.md)
- [Field injection](readme/field-injection.md)
- [Method injection](readme/method-injection.md)
- [Property injection](readme/property-injection.md)
- [Complex generics](readme/complex-generics.md)
- [Instance Initialization](readme/instance-initialization.md)
- [Partial class](readme/partial-class.md)
- [A few partial classes](readme/a-few-partial-classes.md)
- [Dependent compositions](readme/dependent-compositions.md)
- [Default values](readme/default-values.md)
- [Required properties or fields](readme/required-properties-or-fields.md)
### Lifetimes
- [Singleton](readme/singleton.md)
- [PerResolve](readme/perresolve.md)
- [PerBlock](readme/perblock.md)
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
- [Weak Reference](readme/weak-reference.md)
- [Async Enumerable](readme/async-enumerable.md)
- [Service collection](readme/service-collection.md)
- [Func with arguments](readme/func-with-arguments.md)
- [Service provider](readme/service-provider.md)
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
- Console
  - [Schrödinger's cat](readme/Console.md)
  - [Top level statements](readme/ConsoleTopLevelStatements.md)
  - [Native AOT](readme/ConsoleNativeAOT.md)
- UI
  - [MAUI](readme/Maui.md)
  - [WPF](readme/Wpf.md)
  - [Avalonia](readme/Avalonia.md)
  - [Win Forms Net Core](readme/WinFormsAppNetCore.md)
  - [Win Forms](readme/WinFormsApp.md)
- Web
  - [Web](readme/WebApp.md)
  - [Web API service](readme/WebAPI.md)
  - [gRPC service](readme/GrpcService.md)
  - [Blazor Server](readme/BlazorServerApp.md)
  - [Blazor WebAssembly](readme/BlazorWebAssemblyApp.md)
- Git repo with examples
  - [Schrödinger's cat](https://github.com/DevTeam/Pure.DI.Example) 
  - [How to use Pure.DI to create libraries](https://github.com/DevTeam/Pure.DI.Solution)

## Generated Code

Each generated class, hereafter called a _composition_, must be customized. Setup starts with a call to the `Setup(string compositionTypeName)` method:

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

    public object Resolve(Type type) { ... }

    public object Resolve(Type type, object? tag) { ... }
}
```

</details>

<details>
<summary>Setup arguments</summary>

The first parameter is used to specify the name of the composition class. All sets with the same name will be combined to create one composition class. Alternatively, this name may contain a namespace, e.g. a composition class is generated for `Sample.Composition`:

```c#
namespace Sample
{
    partial class Composition
    {
        ...
    }
}
```

The second optional parameter may have multiple values to determine the kind of composition.

### CompositionKind.Public

This value is used by default. If this value is specified, a normal composition class will be created.

### CompositionKind.Internal

If you specify this value, the class will not be generated, but this setup can be used by others as a base setup. For example:

```c#
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind<IDependency>().To<Dependency>();

DI.Setup("Composition").DependsOn("BaseComposition")
    .Bind<IService>().To<Service>();    
```

If the _CompositionKind.Public_ flag is set in the composition setup, it can also be the base for other compositions, as in the example above.

### CompositionKind.Global

No composition class will be created when this value is specified, but this setup is the base setup for all setups in the current project, and `DependsOn(...)` is not required.

</details>

<details>
<summary>Constructors</summary>

### Default constructor

It's quite trivial, this constructor simply initializes the internal state.

### Argument constructor

It replaces the default constructor and is only created if at least one argument is specified. For example:

```c#
DI.Setup("Composition")
    .Arg<string>("name")
    .Arg<int>("id")
    ...
```

In this case, the constructor with arguments is as follows:

```c#
public Composition(string name, int id) { ... }
```

and there is no default constructor. It is important to remember that only those arguments that are used in the object graph will appear in the constructor. Arguments that are not involved cannot be defined, as they are omitted from the constructor parameters to save resources.

### Child constructor

This constructor is always available and is used to create a child composition based on the parent composition:

```c#
var parentComposition = new Composition();
var childComposition = new Composition(parentComposition); 
```

The child composition inherits the state of the parent composition in the form of arguments and singleton objects. The states are copied, and the compositions are completely independent. All singleton objects previously created in the parent composition are also made available in the child composition.

</details>

<details>
<summary>Properties</summary>

### Public Composition Roots

To create an object graph quickly and conveniently, a set of properties (or a methods) is formed. These properties are here called roots of compositions. The type of a property/method is the type of the root object created by the composition. Accordingly, each invocation of a property/method leads to the creation of a composition with a root element of this type.

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

In this case, the property for the _IService_ type will be named _MyService_ and will be available for direct use. The result of its use will be the creation of a composition of objects with the root of _IService_ type:

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

If the root name is empty, a private composition root with a random name is created:

```c#
private IService RootM07D16di_0001
{
    get { ... }
}
```

This root is available in _Resolve_ methods in the same way as public roots. For example:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>();
```

These properties have an arbitrary name and access modifier _private_ and cannot be used directly from the code. Do not attempt to use them, as their names are arbitrarily changed. Private composition roots can be resolved by _Resolve_ methods.

</details>

<details>
<summary>Methods</summary>

### Resolve

By default, a set of four _Resolve_ methods is generated:

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods can resolve both public and private composition roots that do not depend on any arguments of the composition roots. They are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach, where the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is a [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) way to create composition roots. To control the generation of these methods, see the [Resolve](#resolve-hint) hint.

### Dispose

Provides a mechanism to release unmanaged resources. This method is generated only if the composition contains at least one singleton instance that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, the `Dispose()` method of the composition should be called:

```c#
using var composition = new Composition();
```

</details>

<details>
<summary>Setup hints</summary>

## Setup hints

Hints are used to fine-tune code generation. Setup hints can be used as shown in the following example:

```c#
DI.Setup("Composition")
    .Hint(Hint.Resolve, "Off")
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.ToString, "On")
    ...
```

In addition, setup hints can be commented out before the _Setup_ method as `hint = value`. For example:

```c#
// Resolve = Off
// ThreadSafe = Off
DI.Setup("Composition")
    .Hint(Hint.ToString, "On")
    ...
```

Both approaches can be used in combination with each other.

| Hint                                                                                                                               | Values             | Default   | C# version |
|------------------------------------------------------------------------------------------------------------------------------------|--------------------|-----------|------------|
| [Resolve](#resolve-hint)                                                                                                           | _On_ or _Off_      | _On_      |            |
| [OnNewInstance](#onnewinstance-hint)                                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnNewInstanceImplementationTypeNameRegularExpression](#onnewinstanceimplementationtypenameregularexpression-hint)                 | Regular expression | .+        |            |
| [OnNewInstanceTagRegularExpression](#onnewinstancetagregularexpression-hint)                                                       | Regular expression | .+        |            |
| [OnNewInstanceLifetimeRegularExpression](#onnewinstancelifetimeregularexpression-hint)                                             | Regular expression | .+        |            |
| [OnDependencyInjection](#ondependencyinjection-hint)                                                                               | _On_ or _Off_      | _Off_     | 9.0        | 
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression | .+        |            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#ondependencyinjectioncontracttypenameregularexpression-hint)             | Regular expression | .+        |            |
| [OnDependencyInjectionTagRegularExpression](#ondependencyinjectiontagregularexpression-hint)                                       | Regular expression | .+        |            |
| [OnDependencyInjectionLifetimeRegularExpression](#ondependencyinjectionlifetimeregularexpression-hint)                             | Regular expression | .+        |            |
| [OnCannotResolve](#oncannotresolve-hint)                                                                                           | _On_ or _Off_      | _Off_     | 9.0        |
| [OnCannotResolveContractTypeNameRegularExpression](#oncannotresolvecontracttypenameregularexpression-hint)                         | Regular expression | .+        |            |
| [OnCannotResolveTagRegularExpression](#oncannotresolvetagregularexpression-hint)                                                   | Regular expression | .+        |            |
| [OnCannotResolveLifetimeRegularExpression](#oncannotresolvelifetimeregularexpression-hint)                                         | Regular expression | .+        |            |
| [OnNewRoot](#onnewroot-hint)                                                                                                       | _On_ or _Off_      | _Off_     |            |
| [ToString](#tostring-hint)                                                                                                         | _On_ or _Off_      | _Off_     |            |
| [ThreadSafe](#threadsafe-hint)                                                                                                     | _On_ or _Off_      | _On_      |            |
| [ResolveMethodModifiers](#resolvemethodmodifiers-hint)                                                                             | Method modifier    | _public_  |            |
| [ResolveMethodName](#resolvemethodname-hint)                                                                                       | Method name        | _Resolve_ |            |
| [ResolveByTagMethodModifiers](#resolvebytagmethodmodifiers-hint)                                                                   | Method modifier    | _public_  |            |
| [ResolveByTagMethodName](#resolvebytagmethodname-hint)                                                                             | Method name        | _Resolve_ |            |
| [ObjectResolveMethodModifiers](#objectresolvemethodmodifiers-hint)                                                                 | Method modifier    | _public_  |            |
| [ObjectResolveMethodName](#objectresolvemethodname-hint)                                                                           | Method name        | _Resolve_ |            |
| [ObjectResolveByTagMethodModifiers](#objectresolvebytagmethodmodifiers-hint)                                                       | Method modifier    | _public_  |            |
| [ObjectResolveByTagMethodName](#objectresolvebytagmethodname-hint)                                                                 | Method name        | _Resolve_ |            |
| [DisposeMethodModifiers](#disposemethodmodifiers-hint)                                                                             | Method modifier    | _public_  |            |
| [FormatCode](#formatcode-hint)                                                                                                     |  _On_ or _Off_     | _Off_     |            |

The list of hints will be gradually expanded to meet the needs and desires for fine-tuning code generation. Please feel free to add your ideas.

### Resolve Hint

Determines whether to generate [_Resolve_ methods](#resolve). By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce the generation time of the class composition, and in this case no [private composition roots](#private-composition-roots) will be generated. The class composition will be smaller and will only have [public roots](#public-composition-roots). When the _Resolve_ hint is disabled, only the public roots properties are available, so be sure to explicitly define them using the `Root<T>(string name)` method with an explicit composition root name.

### OnNewInstance Hint

Determines whether to generate the _OnNewInstance_ partial method. By default, this partial method is not generated. This can be useful, for example, for logging purposes:

```c#
internal partial class Composition
{
    partial void OnNewInstance<T>(ref T value, object? tag, object lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance with a `T` type, where `T` is the actual type of the created instance. To minimize performance loss when calling _OnNewInstance_, use the three hints below.

### OnNewInstanceImplementationTypeNameRegularExpression Hint

This is a regular expression for filtering by instance type name. This hint is useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of types for which the _OnNewInstance_ method will be called.

### OnNewInstanceTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnNewInstance_ method will be called.

### OnNewInstanceLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to restrict the set of _life_ times for which the _OnNewInstance_ method will be called.

### OnDependencyInjection Hint

Determines whether to generate the _OnDependencyInjection_ partial method to control dependency injection. By default, this partial method is not generated. It cannot have an empty body because of the return value. It must be overridden when it is generated. This may be useful, for example, for [Interception Scenario](readme/interception.md).

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize performance loss when calling _OnDependencyInjection_, use the three tips below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

This is a regular expression for filtering by instance type name. This hint is useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of types for which the _OnDependencyInjection_ method will be called.

### OnDependencyInjectionContractTypeNameRegularExpression Hint

This is a regular expression for filtering by the name of the resolving type. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to limit the set of permissive types for which the _OnDependencyInjection_ method will be called.

### OnDependencyInjectionTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tags_ for which the _OnDependencyInjection_ method will be called.

### OnDependencyInjectionLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of _lifetime_ for which the _OnDependencyInjection_ method will be called.

### OnCannotResolve Hint

Determines whether to generate the `OnCannotResolve<T>(...)` partial method to handle a scenario in which an instance cannot be resolved. By default, this partial method is not generated. Because of the return value, it cannot have an empty body and must be overridden at creation.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing failed bindings by mistake, use the two relevant hints below.

### OnNewRoot Hint

Determines whether to generate a static partial method `OnNewRoot<TContract, T>(...)` to handle the new composition root registration event.

```c#
// OnNewRoot = On
DI.Setup("Composition")
    ...
```

Be careful, this hint disables checks for the ability to resolve dependencies!

### OnCannotResolveContractTypeNameRegularExpression Hint

This is a regular expression for filtering by the name of the resolving type. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of resolving types for which the _OnCannotResolve_ method will be called.

### OnCannotResolveTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnCannotResolve_ method will be called.

### OnCannotResolveLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnCannotResolve_ is in the _On_ state and it is necessary to restrict the set of _lives_ for which the _OnCannotResolve_ method will be called.

### ToString Hint

Determines whether to generate the _ToString()_ method. This method provides a textual class diagram in [mermaid](https://mermaid.js.org/) format. To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/).

```c#
// ToString = On
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
    
var composition = new Composition();
string classDiagram = composition.ToString(); 
```

### ThreadSafe Hint

This hint determines whether the composition of objects will be created in a thread-safe way. The default value of this hint is _On_. It is a good practice not to use threads when creating an object graph, in this case the hint can be disabled, which will result in a small performance gain. For example:

```c#
// ThreadSafe = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

### ResolveMethodModifiers Hint

Overrides the modifiers of the `public T Resolve<T>()` method.

### ResolveMethodName Hint

Overrides the method name for `public T Resolve<T>()`.

### ResolveByTagMethodModifiers Hint

Overrides the modifiers of the `public T Resolve<T>(object? tag)` method.

### ResolveByTagMethodName Hint

Overrides the method name for `public T Resolve<T>(object? tag)`.

### ObjectResolveMethodModifiers Hint

Overrides the modifiers of the `public object Resolve(Type type)` method.

### ObjectResolveMethodName Hint

Overrides the method name for `public object Resolve(Type type)`.

### ObjectResolveByTagMethodModifiers Hint

Overrides the modifiers of the `public object Resolve(Type type, object? tag)` method.

### ObjectResolveByTagMethodName Hint

Overrides the method name for `public object Resolve(Type type, object? tag)`.

### DisposeMethodModifiers Hint

Overrides the modifiers of the `public void Dispose()` method.

### FormatCode Hint

Specifies whether the generated code should be formatted. This option consumes a lot of CPU resources. This hint may be useful when studying the generated code or, for example, when making presentations.

</details>

## NuGet packages

|                   |                                                                                                               |                                                            |
|-------------------|---------------------------------------------------------------------------------------------------------------|:-----------------------------------------------------------|
| Pure.DI           | [![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)                     | DI Source code generator                                   |
| Pure.DI.Templates | [![NuGet](https://buildstats.info/nuget/Pure.DI.Templates)](https://www.nuget.org/packages/Pure.DI.Templates) | Template Package you can call from the shell/command line. |
| Pure.DI.MS        | [![NuGet](https://buildstats.info/nuget/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS)               | Tools for working with Microsoft DI                        |

## Requirements for development environments

- [.NET SDK 6.0.4xx or newer](https://dotnet.microsoft.com/download/dotnet/6.0)
- [C# 8 or newer](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-80)

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

For more information about the template, please see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates).

## Troubleshooting

<details>
<summary>Version update</summary>

When updating the version, it is possible that the previous version of the code generator remains active and is used by compilation services. In this case, the old and new versions of the generator may conflict. For a project where the code generator is used, it is recommended to do the following:
- After updating the version, close the IDE if it is open
- Delete the _obj_ and _bin_ directories
- Execute the following commands one by one

```shell
dotnet build-server shutdown
```

```shell
dotnet restore
```

```shell
dotnet build
```

</details>

<details>
<summary>Disabling API generation</summary>

_Pure.DI_ automatically generates its API. If an assembly already has the _Pure.DI_ API, for example, from another assembly, it is sometimes necessary to disable its automatic generation to avoid ambiguity. To do this, you need to add a _DefineConstants_ element to the project files of these modules. For example:

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);PUREDI_API_SUPPRESSION</DefineConstants>
</PropertyGroup>
```

</details>

<details>
<summary>Display generated files</summary>

You can set project properties to save generated files and control their storage location. In the project file, add the `<EmitCompilerGeneratedFiles>` element to the `<PropertyGroup>` group and set its value to `true`. Build the project again. The generated files are now created in the _obj/Debug/netX.X/generated/Pure.DI/Pure.DI/Pure.DI.SourceGenerator_ directory. The path components correspond to the build configuration, the target framework, the source generator project name, and the full name of the generator type. You can choose a more convenient output folder by adding the `<CompilerGeneratedFilesOutputPath>` element to the application project file. For example:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
    </PropertyGroup>
    
</Project>
```

</details>

## Contribution

Thank you for your interest in contributing to the _Pure.DI_ project! First of all, if you are going to make a big change or feature, please open a problem first. That way, we can coordinate and understand if the change you're going to work on fits with current priorities and if we can commit to reviewing and merging it within a reasonable timeframe. We don't want you to waste a lot of your valuable time on something that may not align with what we want for _Pure.DI_.

This project uses the "build as code" approach using [csharp-interactive](https://github.com/DevTeam/csharp-interactive). The entire build logic is a regular [console .NET application](/build). You can use the [build.cmd](/build.cmd) and [build.sh](/build.sh) files with the appropriate command in the parameters to perform all basic actions on the project, e.g:

- _readme_ - generates README.MD
- _pack_ - creates NuGet packages
- _benchmarks_ - runs benchmark tests

If you are using the Rider IDE, it already has a set of configurations to run these commands.

### Contribution Prerequisites

Installed [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)


## Benchmarks

<details>
<summary>Transient</summary>

<table>
<thead><tr><th>Method              </th><th>Mean    </th><th>Error  </th><th>StdDev  </th><th>Median  </th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Pure.DI composition root&#39;</td><td>2.807 ns</td><td>0.0642 ns</td><td>0.0601 ns</td><td>2.788 ns</td><td>0.91</td><td>0.03</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>3.094 ns</td><td>0.0856 ns</td><td>0.0759 ns</td><td>3.077 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>4.256 ns</td><td>0.1074 ns</td><td>0.1358 ns</td><td>4.236 ns</td><td>1.37</td><td>0.06</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>5.225 ns</td><td>0.1382 ns</td><td>0.1226 ns</td><td>5.196 ns</td><td>1.69</td><td>0.06</td>
</tr><tr><td>LightInject</td><td>6.533 ns</td><td>0.0650 ns</td><td>0.0507 ns</td><td>6.547 ns</td><td>2.11</td><td>0.07</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>7.974 ns</td><td>0.0794 ns</td><td>0.0742 ns</td><td>7.953 ns</td><td>2.58</td><td>0.07</td>
</tr><tr><td>DryIoc</td><td>9.074 ns</td><td>0.1408 ns</td><td>0.1175 ns</td><td>9.061 ns</td><td>2.93</td><td>0.08</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>11.827 ns</td><td>0.1604 ns</td><td>0.1339 ns</td><td>11.809 ns</td><td>3.83</td><td>0.10</td>
</tr><tr><td>Unity</td><td>4,630.964 ns</td><td>92.1425 ns</td><td>213.5543 ns</td><td>4,556.618 ns</td><td>1,602.44</td><td>81.03</td>
</tr><tr><td>Autofac</td><td>10,010.291 ns</td><td>97.0520 ns</td><td>86.0341 ns</td><td>9,986.434 ns</td><td>3,237.55</td><td>80.95</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>19,726.745 ns</td><td>216.7254 ns</td><td>202.7251 ns</td><td>19,745.923 ns</td><td>6,385.97</td><td>175.51</td>
</tr><tr><td>Ninject</td><td>118,514.248 ns</td><td>6,058.9369 ns</td><td>17,384.2256 ns</td><td>117,667.383 ns</td><td>40,179.25</td><td>7,865.92</td>
</tr></tbody></table>

[Transient details](readme/TransientDetails.md)

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>Method              </th><th>Mean   </th><th>Error  </th><th>StdDev </th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Pure.DI composition root&#39;</td><td>2.785 ns</td><td>0.0604 ns</td><td>0.0565 ns</td><td>0.97</td><td>0.05</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>2.864 ns</td><td>0.0874 ns</td><td>0.1006 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>4.251 ns</td><td>0.0458 ns</td><td>0.0406 ns</td><td>1.49</td><td>0.05</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>5.102 ns</td><td>0.1278 ns</td><td>0.1133 ns</td><td>1.79</td><td>0.07</td>
</tr><tr><td>DryIoc</td><td>9.568 ns</td><td>0.2327 ns</td><td>0.2285 ns</td><td>3.33</td><td>0.17</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>11.338 ns</td><td>0.1746 ns</td><td>0.1548 ns</td><td>3.97</td><td>0.17</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>12.776 ns</td><td>0.0654 ns</td><td>0.0579 ns</td><td>4.48</td><td>0.15</td>
</tr><tr><td>LightInject</td><td>305.378 ns</td><td>5.8756 ns</td><td>5.7706 ns</td><td>106.36</td><td>3.37</td>
</tr><tr><td>Unity</td><td>3,282.282 ns</td><td>54.7486 ns</td><td>56.2227 ns</td><td>1,142.80</td><td>46.12</td>
</tr><tr><td>Autofac</td><td>6,841.743 ns</td><td>136.3753 ns</td><td>167.4811 ns</td><td>2,397.47</td><td>93.99</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>10,209.846 ns</td><td>193.5932 ns</td><td>207.1426 ns</td><td>3,556.97</td><td>139.93</td>
</tr><tr><td>Ninject</td><td>47,831.864 ns</td><td>1,548.8960 ns</td><td>4,493.6282 ns</td><td>17,347.25</td><td>1,433.97</td>
</tr></tbody></table>

[Singleton details](readme/SingletonDetails.md)

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>Method              </th><th>Mean  </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.451 ns</td><td>0.1094 ns</td><td>0.1124 ns</td><td>0.91</td><td>0.03</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>3.787 ns</td><td>0.0653 ns</td><td>0.0611 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>4.282 ns</td><td>0.0643 ns</td><td>0.0570 ns</td><td>1.13</td><td>0.01</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>5.588 ns</td><td>0.0282 ns</td><td>0.0250 ns</td><td>1.48</td><td>0.03</td>
</tr><tr><td>DryIoc</td><td>22.018 ns</td><td>0.3400 ns</td><td>0.3180 ns</td><td>5.82</td><td>0.08</td>
</tr><tr><td>LightInject</td><td>108.210 ns</td><td>1.0458 ns</td><td>0.9271 ns</td><td>28.60</td><td>0.39</td>
</tr><tr><td>Unity</td><td>1,801.795 ns</td><td>22.1099 ns</td><td>19.5998 ns</td><td>476.25</td><td>9.66</td>
</tr><tr><td>Autofac</td><td>3,608.847 ns</td><td>72.1798 ns</td><td>67.5171 ns</td><td>953.24</td><td>23.00</td>
</tr></tbody></table>

[Func details](readme/FuncDetails.md)

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>Method              </th><th>Mean  </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>50.09 ns</td><td>0.937 ns</td><td>0.831 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>51.93 ns</td><td>0.845 ns</td><td>0.830 ns</td><td>1.04</td><td>0.02</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>52.11 ns</td><td>0.681 ns</td><td>0.569 ns</td><td>1.04</td><td>0.01</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>53.61 ns</td><td>0.709 ns</td><td>0.663 ns</td><td>1.07</td><td>0.01</td>
</tr><tr><td>LightInject</td><td>55.00 ns</td><td>0.815 ns</td><td>0.636 ns</td><td>1.10</td><td>0.02</td>
</tr><tr><td>DryIoc</td><td>59.06 ns</td><td>1.220 ns</td><td>1.935 ns</td><td>1.20</td><td>0.03</td>
</tr><tr><td>Unity</td><td>4,450.31 ns</td><td>81.849 ns</td><td>109.266 ns</td><td>89.73</td><td>2.39</td>
</tr><tr><td>Autofac</td><td>10,455.41 ns</td><td>201.429 ns</td><td>197.830 ns</td><td>208.81</td><td>6.12</td>
</tr></tbody></table>

[Array details](readme/ArrayDetails.md)

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>Method              </th><th>Mean </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>40.28 ns</td><td>0.333 ns</td><td>0.278 ns</td><td>0.95</td><td>0.01</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>40.56 ns</td><td>0.678 ns</td><td>0.566 ns</td><td>0.95</td><td>0.01</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>42.52 ns</td><td>0.241 ns</td><td>0.214 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>42.95 ns</td><td>0.364 ns</td><td>0.323 ns</td><td>1.01</td><td>0.01</td>
</tr><tr><td>LightInject</td><td>92.13 ns</td><td>0.598 ns</td><td>0.560 ns</td><td>2.17</td><td>0.02</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>92.87 ns</td><td>1.568 ns</td><td>1.390 ns</td><td>2.18</td><td>0.04</td>
</tr><tr><td>DryIoc</td><td>94.00 ns</td><td>1.848 ns</td><td>1.729 ns</td><td>2.21</td><td>0.04</td>
</tr><tr><td>Unity</td><td>2,951.22 ns</td><td>40.251 ns</td><td>33.612 ns</td><td>69.40</td><td>0.78</td>
</tr><tr><td>Autofac</td><td>9,665.00 ns</td><td>79.519 ns</td><td>74.382 ns</td><td>227.13</td><td>1.66</td>
</tr></tbody></table>

[Enum details](readme/EnumDetails.md)

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
12th Gen Intel Core i7-12800H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
</code></pre>

</details>
