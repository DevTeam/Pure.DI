# Pure DI for .NET

_Supports .NET starting with .NET Framework 2.0, released 2005-10-27, and all newer versions._

<a href="https://t.me/pure_di"><img src="https://github.com/DevTeam/Pure.DI/blob/master/readme/telegram.png" align="left" height="20" width="20" ></a>
[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon)](https://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)
[![Build](https://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_PerformanceTests)/statusIcon)](https://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_PerformanceTests&guest=1)

![](readme/di.gif)

## Usage requirements

- Installed [.NET SDK 6.0.4](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- Using [C# 8](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-80) or later

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

enum State { Alive, Dead }
```

### Here's our implementation

```c#
class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;    
}

class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;    
}

```

> [!IMPORTANT]
> Our abstraction and implementation knows nothing about the magic of DI or any frameworks.

### Let's glue it all together

Add the _Pure.DI_ package to your project:

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

Let's bind the abstractions to their implementations and set up the creation of the object graph:

```c#
DI.Setup(nameof(Composition))
    // Models a random subatomic event that may or may not occur
    .Bind().As(Singleton).To<Random>()
    // Quantum superposition of two states: Alive or Dead
    .Bind().To(ctx =>
    {
      ctx.Inject<Random>(out var random);
      return (State)random.Next(2);
    })
    .Bind().To<ShroedingersCat>()
    // Cardboard box with any contents
    .Bind().To<CardboardBox<TT>>()
    // Composition Root
    .Root<Program>("Root");
```

The above code specifies the generation of a partial class named *__Composition__*, this name is defined in the `DI.Setup(nameof(Composition))` call. This class contains a *__Root__* property that returns a graph of objects with an object of type *__Program__* as the root. The type and name of the property is defined by calling `Root<Program>("Root")`. The code of the generated class looks as follows:

```c#
partial class Composition
{
    private object _lock = new object();
    private Random? _random;    
    
    public Program Root
    {
      get
      {
        var stateFunc = new Func<State>(() =>
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

> [!TIP]
> _Pure.DI_ creates efficient code in a pure DI paradigm, using only basic language constructs as if you were writing code by hand. This allows you to take full advantage of Dependency Injection everywhere and always, without any compromise!

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
- [Class arguments](readme/class-arguments.md)
- [Root arguments](readme/root-arguments.md)
- [Tags](readme/tags.md)
- [Field injection](readme/field-injection.md)
- [Method injection](readme/method-injection.md)
- [Property injection](readme/property-injection.md)
- [Default values](readme/default-values.md)
- [Required properties or fields](readme/required-properties-or-fields.md)
- [Root binding](readme/root-binding.md)
- [Async Root](readme/async-root.md)
### Lifetimes
- [Transient](readme/transient.md)
- [Singleton](readme/singleton.md)
- [PerResolve](readme/perresolve.md)
- [PerBlock](readme/perblock.md)
- [Scope](readme/scope.md)
- [Auto scoped](readme/auto-scoped.md)
- [Default lifetime](readme/default-lifetime.md)
- [Disposable singleton](readme/disposable-singleton.md)
- [Async disposable singleton](readme/async-disposable-singleton.md)
- [Async disposable scope](readme/async-disposable-scope.md)
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
- [Generic async composition roots with constraints](readme/generic-async-composition-roots-with-constraints.md)
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
- [Tag Type](readme/tag-type.md)
- [Tag Unique](readme/tag-unique.md)
- [A few partial classes](readme/a-few-partial-classes.md)
- [Partial class](readme/partial-class.md)
- [Dependent compositions](readme/dependent-compositions.md)
- [Accumulators](readme/accumulators.md)
- [Global compositions](readme/global-compositions.md)
- [Tracking async disposable instances in delegates](readme/tracking-async-disposable-instances-in-delegates.md)
- [Tracking disposable instances in delegates](readme/tracking-disposable-instances-in-delegates.md)
- [Tracking disposable instances per a composition root](readme/tracking-disposable-instances-per-a-composition-root.md)
- [Tracking async disposable instances per a composition root](readme/tracking-async-disposable-instances-per-a-composition-root.md)
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
  - [How to use Pure.DI to create and test libraries](https://github.com/DevTeam/Pure.DI.Solution) 
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

and there is no default constructor. 

> [!IMPORTANT]
> It is important to remember that only those arguments that are used in the object graph will appear in the constructor. Arguments that are not involved cannot be defined, as they are omitted from the constructor parameters to save resources.

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
| [DisposeAsyncMethodModifiers](#disposeasyncmethodmodifiers-hint)                                                                   | Method modifier                            |            | _public_  |
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

### DisposeAsyncMethodModifiers Hint

Overrides the modifiers of the `public ValueTask DisposeAsync()` method.

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

| Command        | Description                                                       |
|----------------|-------------------------------------------------------------------|
| g, generator   | Builds and tests generator                                        |
| l, libs        | Builds and tests libraries                                        |
| c, check       | Compatibility checks                                              |
| p, pack        | Creates NuGet packages                                            |
| r, readme      | Generates README.md                                               |
| benchmarks, bm | Runs benchmarks                                                   |
| deploy, dp     | Deploys packages                                                  |
| t, template    | Creates and deploys templates                                     |
| u, upgrade     | Upgrading the internal version of DI to the latest public version |

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
<summary>Array</summary>

<table>
<thead><tr><th>Method              </th><th>Mean </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>168.1 ns</td><td>1.72 ns</td><td>1.44 ns</td><td>1.00</td><td>0.00</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>169.6 ns</td><td>3.08 ns</td><td>2.58 ns</td><td>1.01</td><td>0.02</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>170.7 ns</td><td>3.02 ns</td><td>2.52 ns</td><td>1.02</td><td>0.02</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>171.2 ns</td><td>2.44 ns</td><td>2.04 ns</td><td>1.02</td><td>0.02</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>179.8 ns</td><td>3.43 ns</td><td>3.21 ns</td><td>1.07</td><td>0.02</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>200.5 ns</td><td>3.89 ns</td><td>4.16 ns</td><td>1.20</td><td>0.02</td><td>0.0336</td><td>-</td><td>632 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>9,966.0 ns</td><td>80.16 ns</td><td>62.58 ns</td><td>59.26</td><td>0.42</td><td>0.7629</td><td>-</td><td>14520 B</td><td>22.97</td>
</tr><tr><td>Autofac</td><td>27,170.5 ns</td><td>308.31 ns</td><td>257.45 ns</td><td>161.60</td><td>1.87</td><td>1.5259</td><td>0.0610</td><td>28816 B</td><td>45.59</td>
</tr></tbody></table>

[Array details](readme/ArrayDetails.md)

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>Method              </th><th>Mean </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>128.2 ns</td><td>1.50 ns</td><td>1.25 ns</td><td>0.91</td><td>0.01</td><td>0.0184</td><td>-</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>130.5 ns</td><td>2.22 ns</td><td>1.85 ns</td><td>0.93</td><td>0.02</td><td>0.0184</td><td>-</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>133.8 ns</td><td>2.38 ns</td><td>1.99 ns</td><td>0.95</td><td>0.01</td><td>0.0184</td><td>-</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>140.3 ns</td><td>1.69 ns</td><td>1.41 ns</td><td>1.00</td><td>0.00</td><td>0.0184</td><td>-</td><td>344 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>181.3 ns</td><td>2.01 ns</td><td>1.67 ns</td><td>1.29</td><td>0.02</td><td>0.0250</td><td>-</td><td>472 B</td><td>1.37</td>
</tr><tr><td>LightInject</td><td>277.7 ns</td><td>3.47 ns</td><td>2.89 ns</td><td>1.98</td><td>0.03</td><td>0.0458</td><td>-</td><td>856 B</td><td>2.49</td>
</tr><tr><td>DryIoc</td><td>281.7 ns</td><td>5.33 ns</td><td>4.45 ns</td><td>2.01</td><td>0.03</td><td>0.0458</td><td>-</td><td>856 B</td><td>2.49</td>
</tr><tr><td>Unity</td><td>7,768.1 ns</td><td>124.10 ns</td><td>152.41 ns</td><td>55.43</td><td>1.24</td><td>0.7324</td><td>-</td><td>13752 B</td><td>39.98</td>
</tr><tr><td>Autofac</td><td>26,954.8 ns</td><td>529.73 ns</td><td>543.99 ns</td><td>192.63</td><td>4.87</td><td>1.5259</td><td>0.0610</td><td>28944 B</td><td>84.14</td>
</tr></tbody></table>

[Enum details](readme/EnumDetails.md)

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>Method              </th><th>Mean   </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>8.002 ns</td><td>0.1496 ns</td><td>0.1326 ns</td><td>0.85</td><td>0.01</td><td>0.0013</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>9.445 ns</td><td>0.1727 ns</td><td>0.1615 ns</td><td>1.00</td><td>0.00</td><td>0.0013</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>10.327 ns</td><td>0.2801 ns</td><td>0.2620 ns</td><td>1.09</td><td>0.02</td><td>0.0013</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>11.954 ns</td><td>0.2458 ns</td><td>0.2053 ns</td><td>1.26</td><td>0.03</td><td>0.0013</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>62.785 ns</td><td>1.1086 ns</td><td>0.9828 ns</td><td>6.65</td><td>0.19</td><td>0.0063</td><td>120 B</td><td>5.00</td>
</tr><tr><td>LightInject</td><td>296.638 ns</td><td>3.9363 ns</td><td>3.2870 ns</td><td>31.37</td><td>0.80</td><td>0.0267</td><td>504 B</td><td>21.00</td>
</tr><tr><td>Unity</td><td>4,448.473 ns</td><td>35.3598 ns</td><td>33.0756 ns</td><td>471.11</td><td>9.33</td><td>0.1297</td><td>2552 B</td><td>106.33</td>
</tr><tr><td>Autofac</td><td>10,857.806 ns</td><td>115.9508 ns</td><td>96.8241 ns</td><td>1,147.95</td><td>22.40</td><td>0.7477</td><td>14008 B</td><td>583.67</td>
</tr></tbody></table>

[Func details](readme/FuncDetails.md)

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>Method              </th><th>Mean    </th><th>Error  </th><th>StdDev </th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>7.600 ns</td><td>0.1685 ns</td><td>0.1316 ns</td><td>1.00</td><td>0.00</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>8.652 ns</td><td>0.2436 ns</td><td>0.2392 ns</td><td>1.14</td><td>0.03</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>9.919 ns</td><td>0.2579 ns</td><td>0.2154 ns</td><td>1.30</td><td>0.04</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>11.393 ns</td><td>0.2199 ns</td><td>0.1836 ns</td><td>1.50</td><td>0.04</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>27.581 ns</td><td>0.3758 ns</td><td>0.3138 ns</td><td>3.63</td><td>0.07</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>33.807 ns</td><td>0.3441 ns</td><td>0.3050 ns</td><td>4.45</td><td>0.10</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>38.478 ns</td><td>0.3053 ns</td><td>0.2549 ns</td><td>5.06</td><td>0.07</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>865.775 ns</td><td>1.2567 ns</td><td>1.0494 ns</td><td>113.96</td><td>1.96</td><td>0.0010</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>7,677.459 ns</td><td>99.8336 ns</td><td>88.4999 ns</td><td>1,011.67</td><td>17.40</td><td>0.1678</td><td>-</td><td>3184 B</td><td>132.67</td>
</tr><tr><td>Autofac</td><td>18,825.341 ns</td><td>315.1815 ns</td><td>263.1908 ns</td><td>2,478.00</td><td>62.67</td><td>1.2817</td><td>0.0305</td><td>24208 B</td><td>1,008.67</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>31,318.225 ns</td><td>408.7010 ns</td><td>362.3028 ns</td><td>4,123.56</td><td>74.24</td><td>1.2207</td><td>-</td><td>23912 B</td><td>996.33</td>
</tr><tr><td>Ninject</td><td>116,099.082 ns</td><td>2,240.6371 ns</td><td>2,991.1861 ns</td><td>15,609.54</td><td>360.85</td><td>3.9063</td><td>0.9766</td><td>74096 B</td><td>3,087.33</td>
</tr></tbody></table>

[Singleton details](readme/SingletonDetails.md)

</details>

<details>
<summary>Transient</summary>

<table>
<thead><tr><th>Method              </th><th>Mean    </th><th>Error  </th><th>StdDev </th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</thead><tbody><tr><td>&#39;Pure.DI composition root&#39;</td><td>7.903 ns</td><td>0.1254 ns</td><td>0.0979 ns</td><td>0.97</td><td>0.02</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>8.135 ns</td><td>0.1484 ns</td><td>0.1158 ns</td><td>1.00</td><td>0.00</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>10.337 ns</td><td>0.2882 ns</td><td>0.3848 ns</td><td>1.28</td><td>0.04</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>11.442 ns</td><td>0.2251 ns</td><td>0.1995 ns</td><td>1.41</td><td>0.03</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>LightInject</td><td>20.096 ns</td><td>0.0607 ns</td><td>0.0507 ns</td><td>2.47</td><td>0.04</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Microsoft DI&#39;</td><td>25.671 ns</td><td>0.1087 ns</td><td>0.0849 ns</td><td>3.16</td><td>0.05</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>DryIoc</td><td>27.474 ns</td><td>0.5736 ns</td><td>0.4478 ns</td><td>3.38</td><td>0.07</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>&#39;Simple Injector&#39;</td><td>34.814 ns</td><td>0.2384 ns</td><td>0.2114 ns</td><td>4.28</td><td>0.05</td><td>0.0013</td><td>-</td><td>24 B</td><td>1.00</td>
</tr><tr><td>Unity</td><td>11,090.606 ns</td><td>65.9897 ns</td><td>61.7268 ns</td><td>1,366.08</td><td>19.49</td><td>0.2747</td><td>-</td><td>5176 B</td><td>215.67</td>
</tr><tr><td>Autofac</td><td>28,646.903 ns</td><td>355.7947 ns</td><td>315.4027 ns</td><td>3,520.24</td><td>61.91</td><td>1.7700</td><td>0.0916</td><td>33224 B</td><td>1,384.33</td>
</tr><tr><td>&#39;Castle Windsor&#39;</td><td>58,534.573 ns</td><td>988.3103 ns</td><td>876.1114 ns</td><td>7,196.49</td><td>162.41</td><td>2.8687</td><td>-</td><td>54360 B</td><td>2,265.00</td>
</tr><tr><td>Ninject</td><td>253,820.129 ns</td><td>4,777.4163 ns</td><td>5,310.0855 ns</td><td>31,080.38</td><td>673.81</td><td>6.8359</td><td>1.4648</td><td>131008 B</td><td>5,458.67</td>
</tr></tbody></table>

[Transient details](readme/TransientDetails.md)

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet v0.13.12, Ubuntu 20.04.6 LTS (Focal Fossa)
Intel Xeon Platinum 8259CL CPU 2.50GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 8.0.201
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX-512F+CD+BW+DQ+VL
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX-512F+CD+BW+DQ+VL
</code></pre>

</details>
