# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)
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

## Schrödinger's cat shows how it works [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](samples/ShroedingersCat)

### The reality is

![Cat](readme/cat.png?raw=true)

### Let's create an abstraction

```c#
interface IBox<out T> { T Content { get; } }

interface ICat { State State { get; } }

enum State { Alive, Dead }
```

### Here is our implementation variant

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

It is important to note that our abstraction and implementation knows nothing about the magic of DI or any frameworks. Also note that an instance of type *__Lazy<>__* is used here only as an example. However, using this type with non-trivial logic as a dependency is not recommended, so consider replacing it with some simple abstract type.

### Let's glue it all together

Add a link to the package

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)

For use in the Package Manager

```shell
Install-Package Pure.DI
```

For use in the .NET CLI
  
```shell
dotnet add package Pure.DI
```

Let's bind abstractions to their realizations or factories, define their lifetime and other options:

```c#
partial class Composition
{
  // In fact, this code is never run, and the method can have any name or be a constructor, for example,
  // and can be in any part of the compiled code because this is just a hint to set up an object graph.
  // Here the customization is part of the generated class, just as an example.
  // But in general it can be done anywhere in the code.
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

The above code is just a chain of hints to define the dependency graph used to create a *__Composition__* class with a *__Root__* property that creates the root of the *__Program__* composition below. This code is only useful at compile time, so it can be placed anywhere in the class (in methods, constructors or properties) and preferably where it will not be called at runtime. Its purpose is to check the dependency syntax and help build a compile-time dependency graph to create the *__Composition__* class. The first argument of the _Setup_ method specifies the name of the class to be generated.

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

*__Root__* - here [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) is the only place in the application where object graph composition for the application takes place. Each instance is resolved by a strongly typed block of operators like operator [*__new__*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator), which compile with all optimizations with minimal impact on performance and memory consumption. In general, applications may have several composition roots and, accordingly, such properties. Each composition root must have its own unique name, which is defined when the _Root(string rootName)_ method is called, as shown in the code above. Therefore, the generated _Composition_ class in the example contains only one property _Root_, which is the root of the composition and allows resolving an instance of type _Program_:

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

The full analog of this application with top-level statements can be found [here](Samples/ShroedingersCatTopLevelStatements).

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
- [Tags](readme/tags.md)
- [Auto-bindings](readme/auto-bindings.md)
- [Child composition](readme/child-composition.md)
- [Multi-contract bindings](readme/multi-contract-bindings.md)
- [Field injection](readme/field-injection.md)
- [Method injection](readme/method-injection.md)
- [Property injection](readme/property-injection.md)
- [Complex generics](readme/complex-generics.md)
- [Partial class](readme/partial-class.md)
- [A few partial classes](readme/a-few-partial-classes.md)
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
- [Web API](readme/WebAPITemplate.md)
- [Top level statements console application](readme/ConsoleTopLevelStatementsTemplate.md)

## Generated Code

Each generated class, hereafter called a _composition_, must be customized. It starts with the `Setup(...)` method:

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

If you specify this value, the class will not be generated, but this setup can be used by others as a baseline. For example:

```c#
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind<IDependency>().To<Dependency>();

DI.Setup("Composition").DependsOn("BaseComposition")
    .Bind<IService>().To<Service>();    
```

If the _CompositionKind.Public_ flag is set in the composition setup, it can also be the base for other compositions, as in the example above.

### CompositionKind.Global

No composition class will be created when this value is specified, but this setup is the baseline for all installations in the current project, and `DependsOn(...)` is not required.

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

and there is no default constructor.

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

To create an object graph quickly and conveniently, a set of properties is formed. These properties are here called roots of compositions. The type of a property is the type of the root object created by the composition. Accordingly, each invocation of a property leads to the creation of a composition with a root element of this type.

### Public Composition Roots

To create an object graph quickly and conveniently, a set of properties is formed. These properties are here called roots of compositions. The type of a property is the type of the root object created by the composition. Each invocation of a property results in the creation of a composition with a root element of this type.

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

These properties have an arbitrary name and private accessor and cannot be used directly from the code. Do not attempt to use them, as their names change arbitrarily. Private composition roots can be resolved by _Resolve_ methods.

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

These methods can resolve both public and private composition roots, and are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach, where the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is a [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) way to create composition roots. To control the generation of these methods, see the [Resolve](#resolve-hint) hint.

### Dispose

Provides a mechanism to release unmanaged resources. This method is generated only if the composition contains at least one singleton instance that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, the `Dispose()` method of the composition should be called:

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

Hints are used to fine-tune code generation. Setup hints can be used as shown in the following example:

```c#
DI.Setup("Composition")
    .Hint(Hint.Resolve, "Off")
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.ToString, "On")
    ...
```

In addition, setup hints can be comments before the _Setup_ method, for example, in the form `hint = value`:

```c#
// Resolve = Off
// ThreadSafe = Off
DI.Setup("Composition")
    .Hint(Hint.ToString, "On")
    ...
```

| Hint                                                                                                                               | Values             | Default   | C# version |
|------------------------------------------------------------------------------------------------------------------------------------|--------------------|-----------|------------|
| [Resolve](#resolve-hint)                                                                                                           | _On_ or _Off_      | _On_      |            |
| [OnNewInstance](#onnewinstance-hint)                                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnNewInstanceImplementationTypeNameRegularExpression](#onnewinstanceimplementationtypenameregularexpression-hint)                 | Regular expression | .+        |            |
| [OnNewInstanceTagRegularExpression](#onnewinstancetagregularexpression-hint)                                                       | Regular expression | .+        |            |
| [OnNewInstanceLifetimeRegularExpression](#onnewinstancelifetimeregularexpression-hint)                                             | Regular expression | .+        |            |
| [OnDependencyInjection](#ondependencyinjection-hint)                                                                               | _On_ or _Off_      | _Off_     | 9.0        | | [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression | .+        |            |
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
| [FormatCode](#formatcode-hint)                                                                                                     | _On_ or _Off_      | _Off_     |            |

### Resolve Hint

Determines whether to generate [_Resolve_ methods](#resolve-methods). By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce the generation time of the class composition, and in this case no [private composition roots](#private-roots) will be generated. The class composition will be smaller and will only have [public roots](#public-roots). When the _Resolve_ hint is disabled, only the public roots properties are available, so be sure to explicitly define them using the `Root<T>(string name)` method with an explicit composition root name.

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

## Requirements for development environments

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
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)
- [.NET Multi-platform App UI (MAUI)](https://docs.microsoft.com/en-us/dotnet/maui/)

And others

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

For more information about the template, see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates).

## Troubleshooting

### Disabling API generation

_Pure.DI_ automatically generates its API. If an application module already has the _Pure.DI_ API, its automatic generation for some other modules sometimes needs to be disabled. To do this, add the _DefineConstants_ element to the project files of these modules.

<details>
<summary>For example</summary>

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);PUREDI_API_SUPPRESSION</DefineConstants>
</PropertyGroup>
```

</details>

### Generated files

You can set project properties to save generated files and control their storage location. In the project file, add the `<EmitCompilerGeneratedFiles>` element to the `<PropertyGroup>` group and set its value to `true`. Build the project again. The generated files are now created in the _obj/Debug/netX.X/generated/Pure.DI/Pure.DI/Pure.DI.SourceGenerator_ directory. The path components correspond to the build configuration, the target framework, the source generator project name, and the full name of the generator type. You can choose a more convenient output folder by adding the `<CompilerGeneratedFilesOutputPath>` element to the application project file.

<details>
<summary>For example</summary>

```xml
<Project Sdk="Microsoft.NET.Sdk">
    
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
    </PropertyGroup>
    
</Project>
```

</details>

### Log files

To save the log file, you need to add additional project properties. In the project file, add the `<PureDILogFile>` item to `<PropertyGroup>` and specify the path to the log directory, and add the associated `<CompilerVisibleProperty Include="PureDILogFile" />` item to `<ItemGroup>` to make this property visible in the source code generator. To change the log level, specify it using the _PureDISeverity_ property.

<details>
<summary>For example</summary>

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

The _PureDISeverity_ property can take on multiple values:

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
<thead><tr><th>                Method</th><th>   Mean</th><th>Error</th><th> StdDev</th><th> Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.644 ns</td><td>0.1301 ns</td><td>0.2278 ns</td><td>3.618 ns</td><td>0.95</td><td>0.08</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>3.866 ns</td><td>0.1410 ns</td><td>0.2470 ns</td><td>3.826 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>5.021 ns</td><td>0.1567 ns</td><td>0.3961 ns</td><td>4.893 ns</td><td>1.32</td><td>0.12</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>8.258 ns</td><td>0.2282 ns</td><td>0.3124 ns</td><td>8.227 ns</td><td>2.12</td><td>0.17</td>
</tr><tr><td>LightInject</td><td>11.880 ns</td><td>0.2369 ns</td><td>0.2216 ns</td><td>11.855 ns</td><td>3.09</td><td>0.19</td>
</tr><tr><td>DryIoc</td><td>20.525 ns</td><td>0.4440 ns</td><td>0.8765 ns</td><td>20.342 ns</td><td>5.36</td><td>0.43</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>24.907 ns</td><td>0.3146 ns</td><td>0.2627 ns</td><td>24.797 ns</td><td>6.50</td><td>0.44</td>
</tr><tr><td>SimpleInjector</td><td>25.713 ns</td><td>0.5057 ns</td><td>0.4730 ns</td><td>25.839 ns</td><td>6.68</td><td>0.42</td>
</tr><tr><td>Autofac</td><td>11,836.198 ns</td><td>787.2618 ns</td><td>2,321.2591 ns</td><td>11,580.115 ns</td><td>3,192.58</td><td>703.84</td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>3.786 ns</td><td>0.1347 ns</td><td>0.1752 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>4.313 ns</td><td>0.1508 ns</td><td>0.1961 ns</td><td>1.14</td><td>0.07</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>5.427 ns</td><td>0.1716 ns</td><td>0.3095 ns</td><td>1.43</td><td>0.09</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>8.801 ns</td><td>0.2395 ns</td><td>0.2851 ns</td><td>2.34</td><td>0.15</td>
</tr><tr><td>DryIoc</td><td>22.165 ns</td><td>0.4642 ns</td><td>0.5701 ns</td><td>5.88</td><td>0.31</td>
</tr><tr><td>SimpleInjector</td><td>26.368 ns</td><td>0.3747 ns</td><td>0.3505 ns</td><td>6.96</td><td>0.35</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>27.638 ns</td><td>0.5711 ns</td><td>0.7818 ns</td><td>7.33</td><td>0.36</td>
</tr><tr><td>LightInject</td><td>40.876 ns</td><td>0.6849 ns</td><td>0.7612 ns</td><td>10.82</td><td>0.51</td>
</tr><tr><td>Autofac</td><td>7,092.382 ns</td><td>138.9567 ns</td><td>194.7978 ns</td><td>1,879.76</td><td>116.88</td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>83.26 ns</td><td>1.705 ns</td><td>2.030 ns</td><td>0.95</td><td>0.06</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>85.94 ns</td><td>1.628 ns</td><td>1.444 ns</td><td>0.99</td><td>0.05</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>86.77 ns</td><td>1.785 ns</td><td>4.412 ns</td><td>0.99</td><td>0.07</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>87.80 ns</td><td>1.798 ns</td><td>3.754 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>DryIoc</td><td>105.83 ns</td><td>2.060 ns</td><td>2.606 ns</td><td>1.20</td><td>0.07</td>
</tr><tr><td>LightInject</td><td>432.29 ns</td><td>5.690 ns</td><td>5.322 ns</td><td>4.98</td><td>0.19</td>
</tr><tr><td>Autofac</td><td>8,546.73 ns</td><td>163.729 ns</td><td>175.188 ns</td><td>97.73</td><td>4.80</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>79.99 ns</td><td>2.698 ns</td><td>7.954 ns</td><td>76.70 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>88.79 ns</td><td>2.717 ns</td><td>7.884 ns</td><td>86.02 ns</td><td>1.12</td><td>0.15</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>92.69 ns</td><td>3.242 ns</td><td>9.509 ns</td><td>88.42 ns</td><td>1.17</td><td>0.15</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>93.28 ns</td><td>2.674 ns</td><td>7.884 ns</td><td>90.48 ns</td><td>1.18</td><td>0.14</td>
</tr><tr><td>LightInject</td><td>99.37 ns</td><td>3.547 ns</td><td>10.347 ns</td><td>97.70 ns</td><td>1.25</td><td>0.18</td>
</tr><tr><td>DryIoc</td><td>108.40 ns</td><td>3.156 ns</td><td>9.257 ns</td><td>104.45 ns</td><td>1.37</td><td>0.17</td>
</tr><tr><td>Autofac</td><td>10,368.08 ns</td><td>203.428 ns</td><td>429.098 ns</td><td>10,342.65 ns</td><td>130.12</td><td>15.68</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>196.2 ns</td><td>2.54 ns</td><td>2.38 ns</td><td>0.99</td><td>0.03</td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>198.0 ns</td><td>3.95 ns</td><td>4.85 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI Resolve&lt;T&gt;()&#39;</td><td>202.2 ns</td><td>3.56 ns</td><td>3.33 ns</td><td>1.02</td><td>0.03</td>
</tr><tr><td>&#39;Pure.DI Resolve(Type)&#39;</td><td>205.5 ns</td><td>2.19 ns</td><td>2.05 ns</td><td>1.04</td><td>0.03</td>
</tr><tr><td>DryIoc</td><td>224.1 ns</td><td>2.27 ns</td><td>2.12 ns</td><td>1.13</td><td>0.04</td>
</tr><tr><td>LightInject</td><td>243.4 ns</td><td>4.83 ns</td><td>5.17 ns</td><td>1.23</td><td>0.04</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>248.3 ns</td><td>3.94 ns</td><td>3.69 ns</td><td>1.25</td><td>0.05</td>
</tr><tr><td>Autofac</td><td>10,481.5 ns</td><td>29.76 ns</td><td>26.38 ns</td><td>52.50</td><td>0.95</td>
</tr></tbody></table>

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet v0.13.6, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
Intel Core i7-10875H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 7.0.306
  [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
</code></pre>

</details>
