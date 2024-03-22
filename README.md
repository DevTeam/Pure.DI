# Pure DI for .NET

<a href="https://t.me/pure_di"><img src="https://github.com/DevTeam/Pure.DI/blob/master/readme/telegram.png" align="left" height="20" width="20" ></a>
[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon)](https://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

![](readme/di.gif)

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
  >_Pure.DI_ already [supports](#base-class-library) many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like `Array`, `IEnumerable<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `ISet<T>`, `IProducerConsumerCollection<T>`, `ConcurrentBag<T>`, `Func<T>`, `ThreadLocal`, `ValueTask<T>`, `Task<T>`, `MemoryPool<T>`, `ArrayPool<T>`, `ReadOnlyMemory<T>`, `Memory<T>`, `ReadOnlySpan<T>`, `Span<T>`, `IComparer<T>`, `IEqualityComparer<T>` and etc. without any extra effort.
- [X] Good for building libraries or frameworks where resource consumption is particularly critical.
  >Its high performance, zero memory consumption/preparation overhead, and lack of dependencies make it ideal for building libraries and frameworks.

![](di.gif)

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
  private static void Setup() => 
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind().To(ctx =>
        {
          ctx.Inject<Random>(out var random);
          return (State)random.Next(2);
        })
        .Bind().To<ShroedingersCat>()
        // Represents a cardboard box with any contents
        .Bind().To<CardboardBox<TT>>()
        // Composition Root
        .Root<Program>("Root");
}
```

The above code specifies the generation of a partial class named *__Composition__*, this name is defined in the `DI.Setup(nameof(Composition))` call. This class contains a *__Root__* property that returns a graph of objects with an object of type *__Program__* as the root. The type and name of the property is defined by calling `Root<Program>("Root")`. The code of the generated class looks as follows:

```c#
partial class Composition
{
    private object _lock = new object();
    private Random _random;    
    
    public Program Root
    {
      get
      {
        Func<State> stateFunc = new Func<State>(() =>
        {
          if (_random == null)
          {
            lock (_lock)
            {
              if (_random == null)
              {
                _random = new Random();
              }
            }
          }
          
          return (State)_random.Next(2);      
        });
        
        return new Program(
          new CardboardBox<ICat>(
            new ShroedingersCat(
              new Lazy<Sample.State>(
                stateFunc))));    
      }
    }
    
    public T Resolve<T>() { ... }
    
    public object Resolve(Type type) { ... }    
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
- [Auto-bindings](readme/auto-bindings.md)
- [Injections of abstractions](readme/injections-of-abstractions.md)
- [Composition roots](readme/composition-roots.md)
- [Resolve methods](readme/resolve-methods.md)
- [Simplified binding](readme/simplified-binding.md)
- [Factory](readme/factory.md)
- [Injection](readme/injection.md)
- [Arguments](readme/arguments.md)
- [Root arguments](readme/root-arguments.md)
- [Tags](readme/tags.md)
- [Multi-contract bindings](readme/multi-contract-bindings.md)
- [Field injection](readme/field-injection.md)
- [Method injection](readme/method-injection.md)
- [Property injection](readme/property-injection.md)
- [Default values](readme/default-values.md)
- [Required properties or fields](readme/required-properties-or-fields.md)
- [Tracking disposable instances in delegates](readme/tracking-disposable-instances-in-delegates.md)
- [Tracking disposable instances per a composition root](readme/tracking-disposable-instances-per-a-composition-root.md)
### Lifetimes
- [Singleton](readme/singleton.md)
- [PerResolve](readme/perresolve.md)
- [Scope](readme/scope.md)
- [PerBlock](readme/perblock.md)
- [Transient](readme/transient.md)
- [Disposable singleton](readme/disposable-singleton.md)
- [Auto scoped](readme/auto-scoped.md)
- [Default lifetime](readme/default-lifetime.md)
### Base Class Library
- [Func](readme/func.md)
- [Enumerable](readme/enumerable.md)
- [Enumerable generics](readme/enumerable-generics.md)
- [Array](readme/array.md)
- [Lazy](readme/lazy.md)
- [Task](readme/task.md)
- [ValueTask](readme/valuetask.md)
- [Manually started tasks](readme/manually-started-tasks.md)
- [Span and ReadOnlySpan](readme/span-and-readonlyspan.md)
- [Tuple](readme/tuple.md)
- [Weak Reference](readme/weak-reference.md)
- [Async Enumerable](readme/async-enumerable.md)
- [Service collection](readme/service-collection.md)
- [Func with arguments](readme/func-with-arguments.md)
- [Func with tag](readme/func-with-tag.md)
- [Keyed service provider](readme/keyed-service-provider.md)
- [Service provider](readme/service-provider.md)
- [Service provider with scope](readme/service-provider-with-scope.md)
- [Overriding the BCL binding](readme/overriding-the-bcl-binding.md)
### Generics
- [Generics](readme/generics.md)
- [Generic composition roots](readme/generic-composition-roots.md)
- [Complex generics](readme/complex-generics.md)
- [Generic composition roots with constraints](readme/generic-composition-roots-with-constraints.md)
### Attributes
- [Constructor ordinal attribute](readme/constructor-ordinal-attribute.md)
- [Member ordinal attribute](readme/member-ordinal-attribute.md)
- [Tag attribute](readme/tag-attribute.md)
- [Type attribute](readme/type-attribute.md)
- [Custom attributes](readme/custom-attributes.md)
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
- [Check for a root](readme/check-for-a-root.md)
### Advanced
- [Composition root kinds](readme/composition-root-kinds.md)
- [Instance Initialization](readme/instance-initialization.md)
- [Tag Type](readme/tag-type.md)
- [Tag Unique](readme/tag-unique.md)
- [A few partial classes](readme/a-few-partial-classes.md)
- [Partial class](readme/partial-class.md)
- [Dependent compositions](readme/dependent-compositions.md)
- [Accumulators](readme/accumulators.md)
- [Global compositions](readme/global-compositions.md)
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
  - [Minimal Web API](readme/MinimalWebAPI.md)
  - [Web API](readme/WebAPI.md)
  - [gRPC service](readme/GrpcService.md)
  - [Blazor Server](readme/BlazorServerApp.md)
  - [Blazor WebAssembly](readme/BlazorWebAssemblyApp.md)
    - [https://devteam.github.io/Pure.DI/](https://devteam.github.io/Pure.DI/)
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
    // Default constructor
    public Composition() { }

    // Scope constructor
    internal Composition(Composition baseComposition) { }

    // Composition root
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

### Parameterized constructor

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

### Scope constructor

This constructor creates a composition instance for the new scope. This allows ``Lifetime.Scoped`` to be applied. See [this](readme/scope.md) example for details.

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

| Hint                                                                                                                               | Values                                     | C# version | Default   |
|------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|------------|-----------|
| [Resolve](#resolve-hint)                                                                                                           | _On_ or _Off_                              |            | _On_      |
| [OnNewInstance](#onnewinstance-hint)                                                                                               | _On_ or _Off_                              | 9.0        | _Off_     |
| [OnNewInstancePartial](#onnewinstance-hint)                                                                                        | _On_ or _Off_                              |         | _On_      |
| [OnNewInstanceImplementationTypeNameRegularExpression](#onnewinstanceimplementationtypenameregularexpression-hint)                 | Regular expression                         |            | .+        |
| [OnNewInstanceTagRegularExpression](#onnewinstancetagregularexpression-hint)                                                       | Regular expression                         |            | .+        |
| [OnNewInstanceLifetimeRegularExpression](#onnewinstancelifetimeregularexpression-hint)                                             | Regular expression                         |            | .+        |
| [OnDependencyInjection](#ondependencyinjection-hint)                                                                               | _On_ or _Off_                              | 9.0        | _Off_     | 
| [OnDependencyInjectionPartial](#ondependencyinjectionpartial-hint)                                                                 | _On_ or _Off_                              |         | _On_      |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression                         |            | .+        |
| [OnDependencyInjectionContractTypeNameRegularExpression](#ondependencyinjectioncontracttypenameregularexpression-hint)             | Regular expression                         |            | .+        |
| [OnDependencyInjectionTagRegularExpression](#ondependencyinjectiontagregularexpression-hint)                                       | Regular expression                         |            | .+        |
| [OnDependencyInjectionLifetimeRegularExpression](#ondependencyinjectionlifetimeregularexpression-hint)                             | Regular expression                         |            | .+        |
| [OnCannotResolve](#oncannotresolve-hint)                                                                                           | _On_ or _Off_                              | 9.0        | _Off_     |
| [OnCannotResolvePartial](#oncannotresolvepartial-hint)                                                                             | _On_ or _Off_                              |         | _On_      |
| [OnCannotResolveContractTypeNameRegularExpression](#oncannotresolvecontracttypenameregularexpression-hint)                         | Regular expression                         |            | .+        |
| [OnCannotResolveTagRegularExpression](#oncannotresolvetagregularexpression-hint)                                                   | Regular expression                         |            | .+        |
| [OnCannotResolveLifetimeRegularExpression](#oncannotresolvelifetimeregularexpression-hint)                                         | Regular expression                         |            | .+        |
| [OnNewRoot](#onnewroot-hint)                                                                                                       | _On_ or _Off_                              |            | _Off_     |
| [OnNewRootPartial](#onnewrootpartial-hint)                                                                                         | _On_ or _Off_                              |            | _On_      |
| [ToString](#tostring-hint)                                                                                                         | _On_ or _Off_                              |            | _Off_     |
| [ThreadSafe](#threadsafe-hint)                                                                                                     | _On_ or _Off_                              |            | _On_      |
| [ResolveMethodModifiers](#resolvemethodmodifiers-hint)                                                                             | Method modifier                            |            | _public_  |
| [ResolveMethodName](#resolvemethodname-hint)                                                                                       | Method name                                |            | _Resolve_ |
| [ResolveByTagMethodModifiers](#resolvebytagmethodmodifiers-hint)                                                                   | Method modifier                            |            | _public_  |
| [ResolveByTagMethodName](#resolvebytagmethodname-hint)                                                                             | Method name                                |            | _Resolve_ |
| [ObjectResolveMethodModifiers](#objectresolvemethodmodifiers-hint)                                                                 | Method modifier                            |            | _public_  |
| [ObjectResolveMethodName](#objectresolvemethodname-hint)                                                                           | Method name                                |            | _Resolve_ |
| [ObjectResolveByTagMethodModifiers](#objectresolvebytagmethodmodifiers-hint)                                                       | Method modifier                            |            | _public_  |
| [ObjectResolveByTagMethodName](#objectresolvebytagmethodname-hint)                                                                 | Method name                                |            | _Resolve_ |
| [DisposeMethodModifiers](#disposemethodmodifiers-hint)                                                                             | Method modifier                            |            | _public_  |
| [FormatCode](#formatcode-hint)                                                                                                     | _On_ or _Off_                              |            | _Off_     |
| [SeverityOfNotImplementedContract](#severityofnotimplementedcontract-hint)                                                         | _Error_ or _Warning_ or _Info_ or _Hidden_ |            | _Error_   |
| [Comments](#comments-hint)                                                                                                         | _On_ or _Off_                              |            | _On_     |

The list of hints will be gradually expanded to meet the needs and desires for fine-tuning code generation. Please feel free to add your ideas.

### Resolve Hint

Determines whether to generate [_Resolve_ methods](#resolve). By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce the generation time of the class composition, and in this case no [private composition roots](#private-composition-roots) will be generated. The class composition will be smaller and will only have [public roots](#public-composition-roots). When the _Resolve_ hint is disabled, only the public roots properties are available, so be sure to explicitly define them using the `Root<T>(string name)` method with an explicit composition root name.

### OnNewInstance Hint

Determines whether to use the _OnNewInstance_ partial method. By default, this partial method is not generated. This can be useful, for example, for logging purposes:

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

### OnNewInstancePartial Hint

Determines whether to generate the _OnNewInstance_ partial method. By default, this partial method is generated when the _OnNewInstance_ hint is ```On```.

### OnNewInstanceImplementationTypeNameRegularExpression Hint

This is a regular expression for filtering by instance type name. This hint is useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of types for which the _OnNewInstance_ method will be called.

### OnNewInstanceTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnNewInstance_ method will be called.

### OnNewInstanceLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to restrict the set of _life_ times for which the _OnNewInstance_ method will be called.

### OnDependencyInjection Hint

Determines whether to use the _OnDependencyInjection_ partial method when the _OnDependencyInjection_ hint is ```On``` to control dependency injection. By default it is ```On```.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionPartial = Off
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

### OnDependencyInjectionPartial Hint

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

Determines whether to use the `OnCannotResolve<T>(...)` partial method to handle a scenario in which an instance cannot be resolved. By default, this partial method is not generated. Because of the return value, it cannot have an empty body and must be overridden at creation.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing failed bindings by mistake, use the two relevant hints below.

### OnCannotResolvePartial Hint

Determines whether to generate the `OnCannotResolve<T>(...)` partial method when the _OnCannotResolve_ hint is <c>On</c> to handle a scenario in which an instance cannot be resolved. By default it is ```On```.

```c#
// OnCannotResolve = On
// OnCannotResolvePartial = Off
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing failed bindings by mistake, use the two relevant hints below.

### OnNewRoot Hint

Determines whether to use a static partial method `OnNewRoot<TContract, T>(...)` to handle the new composition root registration event.

```c#
// OnNewRoot = On
DI.Setup("Composition")
    ...
```

Be careful, this hint disables checks for the ability to resolve dependencies!

### OnNewRootPartial Hint

Determines whether to generate a static partial method `OnNewRoot<TContract, T>(...)` when the _OnNewRoot_ hint is ```On``` to handle the new composition root registration event.

```c#
// OnNewRootPartial = Off
DI.Setup("Composition")
    ...
```

### OnCannotResolveContractTypeNameRegularExpression Hint

This is a regular expression for filtering by the name of the resolving type. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of resolving types for which the _OnCannotResolve_ method will be called.

### OnCannotResolveTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnCannotResolve_ method will be called.

### OnCannotResolveLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnCannotResolve_ is in the _On_ state and it is necessary to restrict the set of _lives_ for which the _OnCannotResolve_ method will be called.

### ToString Hint

Determines whether to generate the _ToString()_ method. This method provides a class diagram in [mermaid](https://mermaid.js.org/) format. To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/).

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

### SeverityOfNotImplementedContract Hint

Indicates the severity level of the situation when, in the binding, an implementation does not implement a contract. Possible values:

- _"Error"_, it is default value.
- _"Warning"_ - something suspicious but allowed.
- _"Info"_ - information that does not indicate a problem.
- _"Hidden"_ - what's not a problem.

### Comments Hint

Specifies whether the generated code should be commented.

```c#
// Represents the composition class
DI.Setup(nameof(Composition))
    .Bind<IService>().To<Service>()
    // Provides a composition root of my service
    .Root<IService>("MyService");
```

Appropriate comments will be added to the generated ```Composition``` class and the documentation for the class, depending on the IDE used, will look something like this:

![ReadmeDocumentation1.png](readme/ReadmeDocumentation1.png)

Then documentation for the composition root:

![ReadmeDocumentation2.png](readme/ReadmeDocumentation2.png)

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

| Command        | Description                   |
|----------------|-------------------------------|
| g, generator   | Builds and tests generator    |
| l, libs        | Builds and tests libraries    |
| c, check       | Compatibility checks          |
| p, pack        | Creates NuGet packages        |
| r, readme      | Generates README.md           |
| benchmarks, bm | Runs benchmarks               |
| deploy, dp     | Deploys packages              |
| t, template    | Creates and deploys templates |
| u, update      | Updates internal DI version   |

For example:

```
./build.sh pack
./build.cmd benchmarks
```

If you are using the Rider IDE, it already has a set of configurations to run these commands.

### Contribution Prerequisites

Installed [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)


## Benchmarks

<details>
<summary>Transient</summary>

<table>
<thead><tr><th>Method              </th><th>Mean   </th><th>Error  </th><th>StdDev </th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</thead><tbody><tr><td>&#39;Pure.DI composition root&#39;</td><td>11.71 ns</td><td>0.360 ns</td><td>0.719 ns</td><td>1.01</td><td>0.10</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>11.74 ns</td><td>0.364 ns</td><td>0.880 ns</td><td>1.00</td><td>0.00</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>15.11 ns</td><td>0.428 ns</td><td>0.864 ns</td><td>1.30</td><td>0.12</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>19.97 ns</td><td>0.527 ns</td><td>1.322 ns</td><td>1.71</td><td>0.16</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>25.10 ns</td><td>0.628 ns</td><td>1.677 ns</td><td>2.14</td><td>0.21</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>34.89 ns</td><td>0.821 ns</td><td>2.192 ns</td><td>2.97</td><td>0.28</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>37.01 ns</td><td>0.874 ns</td><td>1.785 ns</td><td>3.18</td><td>0.31</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>47.68 ns</td><td>1.072 ns</td><td>2.670 ns</td><td>4.08</td><td>0.41</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>15,191.23 ns</td><td>300.391 ns</td><td>485.075 ns</td><td>1,343.38</td><td>98.43</td><td>0.1831</td><td>-</td><td>5176 B</td><td>215.67</td>
</tr><tr><td>Autofac</td><td>45,388.57 ns</td><td>901.253 ns</td><td>2,467.165 ns</td><td>3,879.60</td><td>376.82</td><td>1.2207</td><td>0.0610</td><td>33224 B</td><td>1,384.33</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>92,274.63 ns</td><td>2,395.998 ns</td><td>6,353.846 ns</td><td>7,919.73</td><td>844.07</td><td>2.0752</td><td>-</td><td>54360 B</td><td>2,265.00</td>
</tr><tr><td>Ninject</td><td>437,510.11 ns</td><td>11,211.063 ns</td><td>31,251.962 ns</td><td>37,506.08</td><td>3,954.02</td><td>4.8828</td><td>1.4648</td><td>131008 B</td><td>5,458.67</td>
</tr></tbody></table>

[Transient details](readme/TransientDetails.md)

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>Method              </th><th>Mean   </th><th>Error </th><th>StdDev </th><th>Median </th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>11.30 ns</td><td>0.347 ns</td><td>0.950 ns</td><td>11.28 ns</td><td>1.00</td><td>0.00</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>11.83 ns</td><td>0.366 ns</td><td>0.989 ns</td><td>11.94 ns</td><td>1.06</td><td>0.14</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>15.58 ns</td><td>0.438 ns</td><td>0.999 ns</td><td>15.66 ns</td><td>1.41</td><td>0.14</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>19.48 ns</td><td>0.526 ns</td><td>0.907 ns</td><td>19.46 ns</td><td>1.75</td><td>0.19</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>36.15 ns</td><td>0.841 ns</td><td>1.966 ns</td><td>36.16 ns</td><td>3.25</td><td>0.36</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>50.35 ns</td><td>1.132 ns</td><td>2.438 ns</td><td>50.47 ns</td><td>4.56</td><td>0.46</td><td>0.0009</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>54.55 ns</td><td>1.219 ns</td><td>3.168 ns</td><td>54.37 ns</td><td>4.88</td><td>0.54</td><td>0.0008</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>1,167.20 ns</td><td>29.455 ns</td><td>85.455 ns</td><td>1,168.13 ns</td><td>104.39</td><td>12.14</td><td>-</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>13,851.41 ns</td><td>990.752 ns</td><td>2,921.254 ns</td><td>13,388.93 ns</td><td>1,221.03</td><td>270.17</td><td>0.1068</td><td>-</td><td>3184 B</td><td>132.67</td>
</tr><tr><td>Autofac</td><td>29,432.69 ns</td><td>580.743 ns</td><td>1,250.110 ns</td><td>29,628.93 ns</td><td>2,662.95</td><td>238.01</td><td>0.9155</td><td>0.0305</td><td>24208 B</td><td>1,008.67</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>55,866.46 ns</td><td>4,142.818 ns</td><td>12,215.193 ns</td><td>50,217.47 ns</td><td>4,961.92</td><td>1,115.86</td><td>0.8545</td><td>-</td><td>23912 B</td><td>996.33</td>
</tr><tr><td>Ninject</td><td>185,155.88 ns</td><td>5,901.921 ns</td><td>16,156.414 ns</td><td>183,742.97 ns</td><td>16,506.27</td><td>1,997.27</td><td>1.9531</td><td>-</td><td>74097 B</td><td>3,087.38</td>
</tr></tbody></table>

[Singleton details](readme/SingletonDetails.md)

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>Method              </th><th>Mean  </th><th>Error </th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>12.12 ns</td><td>0.454 ns</td><td>1.280 ns</td><td>12.20 ns</td><td>1.00</td><td>0.13</td><td>0.0009</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>12.24 ns</td><td>0.386 ns</td><td>1.070 ns</td><td>12.39 ns</td><td>1.00</td><td>0.00</td><td>0.0009</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>16.70 ns</td><td>0.460 ns</td><td>0.793 ns</td><td>16.70 ns</td><td>1.38</td><td>0.15</td><td>0.0009</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>20.25 ns</td><td>0.531 ns</td><td>1.370 ns</td><td>20.32 ns</td><td>1.67</td><td>0.21</td><td>0.0009</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>99.11 ns</td><td>6.928 ns</td><td>20.428 ns</td><td>90.87 ns</td><td>8.21</td><td>1.87</td><td>0.0045</td><td>120 B</td><td>5.00</td>
</tr><tr><td>LightInject</td><td>456.39 ns</td><td>9.905 ns</td><td>27.281 ns</td><td>454.75 ns</td><td>37.65</td><td>4.58</td><td>0.0191</td><td>504 B</td><td>21.00</td>
</tr><tr><td>Unity</td><td>6,421.69 ns</td><td>128.095 ns</td><td>371.628 ns</td><td>6,498.50 ns</td><td>528.32</td><td>56.74</td><td>0.0916</td><td>2552 B</td><td>106.33</td>
</tr><tr><td>Autofac</td><td>20,047.40 ns</td><td>1,348.433 ns</td><td>3,975.886 ns</td><td>18,347.20 ns</td><td>1,683.65</td><td>428.79</td><td>0.5188</td><td>14008 B</td><td>583.67</td>
</tr></tbody></table>

[Func details](readme/FuncDetails.md)

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>Method              </th><th>Mean </th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>241.2 ns</td><td>6.22 ns</td><td>17.96 ns</td><td>242.1 ns</td><td>1.00</td><td>0.10</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>242.6 ns</td><td>4.94 ns</td><td>11.83 ns</td><td>242.9 ns</td><td>1.00</td><td>0.00</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>250.5 ns</td><td>6.13 ns</td><td>17.60 ns</td><td>250.7 ns</td><td>1.05</td><td>0.09</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>262.6 ns</td><td>6.31 ns</td><td>18.32 ns</td><td>264.5 ns</td><td>1.09</td><td>0.10</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>268.9 ns</td><td>10.84 ns</td><td>29.66 ns</td><td>264.2 ns</td><td>1.09</td><td>0.09</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>271.4 ns</td><td>5.53 ns</td><td>16.05 ns</td><td>271.7 ns</td><td>1.13</td><td>0.09</td><td>0.0238</td><td>632 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>16,147.7 ns</td><td>372.62 ns</td><td>1,038.71 ns</td><td>16,193.5 ns</td><td>66.63</td><td>5.50</td><td>0.5493</td><td>14520 B</td><td>22.97</td>
</tr><tr><td>Autofac</td><td>51,686.3 ns</td><td>3,931.99 ns</td><td>11,593.55 ns</td><td>45,129.5 ns</td><td>229.40</td><td>54.23</td><td>1.0986</td><td>28816 B</td><td>45.59</td>
</tr></tbody></table>

[Array details](readme/ArrayDetails.md)

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>Method              </th><th>Mean </th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>175.8 ns</td><td>3.62 ns</td><td>9.60 ns</td><td>176.1 ns</td><td>0.97</td><td>0.08</td><td>0.0131</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>176.9 ns</td><td>3.61 ns</td><td>8.37 ns</td><td>178.0 ns</td><td>0.98</td><td>0.07</td><td>0.0131</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>181.5 ns</td><td>3.77 ns</td><td>9.92 ns</td><td>180.9 ns</td><td>1.00</td><td>0.00</td><td>0.0131</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>185.6 ns</td><td>3.81 ns</td><td>8.99 ns</td><td>184.7 ns</td><td>1.02</td><td>0.07</td><td>0.0131</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>308.0 ns</td><td>20.95 ns</td><td>61.78 ns</td><td>276.8 ns</td><td>1.76</td><td>0.35</td><td>0.0176</td><td>472 B</td><td>1.37</td>
</tr><tr><td>LightInject</td><td>439.1 ns</td><td>22.65 ns</td><td>66.79 ns</td><td>408.1 ns</td><td>2.42</td><td>0.39</td><td>0.0324</td><td>856 B</td><td>2.49</td>
</tr><tr><td>DryIoc</td><td>484.5 ns</td><td>27.58 ns</td><td>81.32 ns</td><td>466.2 ns</td><td>2.76</td><td>0.46</td><td>0.0324</td><td>856 B</td><td>2.49</td>
</tr><tr><td>Unity</td><td>12,287.5 ns</td><td>245.07 ns</td><td>495.05 ns</td><td>12,357.3 ns</td><td>68.25</td><td>4.84</td><td>0.5188</td><td>13752 B</td><td>39.98</td>
</tr><tr><td>Autofac</td><td>48,070.2 ns</td><td>2,330.57 ns</td><td>6,379.91 ns</td><td>46,378.6 ns</td><td>265.59</td><td>36.70</td><td>1.0986</td><td>28944 B</td><td>84.14</td>
</tr></tbody></table>

[Enum details](readme/EnumDetails.md)

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22000.2538/21H2/SunValley) (Hyper-V)
Intel Xeon CPU E5-2673 v4 2.30GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
</code></pre>

</details>
