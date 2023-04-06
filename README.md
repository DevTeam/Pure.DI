# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon"/>](http://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

<img src="Docs/Images/demo.gif" alt="Demo"/>

## Key features

Pure.DI is __NOT__ a framework or library, but a code generator that generates static method code to create an object graph in a pure DI paradigm using a set of hints that are verified at compile time. Since all the work is done at compile time, at run time you only have efficient code that is ready to be used. This generated code does not depend on library calls or .NET reflection and is efficient in terms of performance and memory consumption.

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

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

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
static class DI
{
  // Actually, this code never runs and the method might have any name or be a constructor for instance
  // because this is just a hint to set up an object graph.
  private static void Setup() => DI.Setup("Composition")
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
- [Default lifetime](readme/Examples.md#default-lifetime)
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
## Composition class

For each generated class, hereinafter referred to as _composition_, the setup must be done:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

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

The second optional parameter can have several values to determine the kind of composition:

| Options                  |                                                                                                                                              |
|--------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| CompositionKind.Public   | Default value. This option will create a composition class.                                                                                  |
| CompositionKind.Internal | If this value is specified, the class will not be generated, but this setup can be used for others as a base.                                |
| CompositionKind.Global   | If this value is specified, the composition class will not be generated, but this setup is a default base for all setups in current project. |

</details>

The composition may contain the following parts:

<details>
<summary>Constructors</summary>

### Constructors

1. Default constructor

   Just initializes the internal state.

2. Argument constructor

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

3. Child constructor

   This constructor is always available and is used to create a child composition based on the parent composition:
   ```c#
   var parentComposition = new Composition();
   var childComposition = new Composition(parentComposition); 
   ```
   The child composition inherits the state of the parent composition in the form of arguments and singleton objects. States are copied, and compositions are completely independent, except when calling the _Dispose()_ method on the parent container before disposing of the child container, because the child container can use singleton objects created before it was created.

</details>

<details>
<summary>Methods to resolve instances</summary>

### _Resolve_

By default a set of four _Resolve_ methods are generated within generated composition class.

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods are useful when using the Service Locator approach when the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

To control the generation of these methods, see [Resolve Hint](#Resolve-Hint).

</details>

<details>
<summary>Roots</summary>

To be able to quickly and conveniently create an object graph, a set of properties is generated. The type of the property is the type of the root object created by the composition. Accordingly, each access to the property leads to the creation of a composition with the root element of this type.

### Private Roots

The composition has properties for each potential root that are used in those _Resolve_ methods. For example:

```c#
private IService Root2PropABB3D0
{
    get { ... }
}
```

These properties have a random name and a private accessor and cannot be used directly from code. Don't try to use them.

### Public Roots

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
        retunr new Service(...);
    }
}
```

The composition can contain any number of roots.

</details>

<details>
<summary>Dispose</summary>

### Dispose method

This method is only generated if the composition contains at least one singleton object that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, call the composition `Dispose()` method:

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

Setup hints are comments before method _Setup_ in the form ```hint = value``` that are used to fine-tune code generation. For example:

```c#
// Resolve = Off
// ThreadSafe = Off
// ToString = On
DI.Setup("Composition")
    ...
```

| Hint                                                                                                                               | Default Value |
|------------------------------------------------------------------------------------------------------------------------------------|---------------|
| [Resolve](#Resolve-Hint)                                                                                                           | On            |
| [OnInstanceCreation](#OnInstanceCreation-Hint)                                                                                     | On            |
| [OnDependencyInjection](#OnDependencyInjection-Hint)                                                                               | Off           |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | .+            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#OnDependencyInjectionContractTypeNameRegularExpression-Hint)             | .+            |
| [OnDependencyInjectionTagRegularExpression](#OnDependencyInjectionTagRegularExpression-Hint)                                       | .+            |
| [ToString](#ToString-Hint)                                                                                                         | Off           |
| [ThreadSafe](#ThreadSafe-Hint)                                                                                                     | On            |

### Resolve Hint

Determine whether to generate [_Resolve_ methods](#resolve-methods). By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no [private composition roots](#Private-Roots) will be generated in this case. The composition will be tiny and will only have [public roots](#Public-Roots).

### OnInstanceCreation Hint

Determine whether to generate partial _OnInstanceCreation_ method. This partial method is generated by default, has no body, and can be overridden as needed. If the body is not defined, then the compiler will cut out its calls. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnInstanceCreation<T>(ref T value, object? tag, object? lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance of type `T`, where `T` is actually type of created instance.

### OnDependencyInjection Hint

Determine whether to generate partial _OnDependencyInjection_ method to control of dependency injection. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden when generated. This can be useful, for example, for [interception](#Interception).

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize the performance penalty when calling _OnDependencyInjection_, use the other hints below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnDependencyInjection_ will be called.

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


## Benchmarks

<details>
<summary>Transient</summary>

<table>
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.1529 ns</td><td>0.0498 ns</td><td>0.1468 ns</td><td>0.0993 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>0.4598 ns</td><td>0.0255 ns</td><td>0.0752 ns</td><td>0.4521 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>6.2269 ns</td><td>0.2886 ns</td><td>0.8371 ns</td><td>6.0107 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>6.3210 ns</td><td>0.2087 ns</td><td>0.5988 ns</td><td>6.1957 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>9.8488 ns</td><td>0.3375 ns</td><td>0.9791 ns</td><td>9.6665 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>12.1087 ns</td><td>0.2613 ns</td><td>0.6930 ns</td><td>12.0452 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>14.5004 ns</td><td>0.3218 ns</td><td>0.9182 ns</td><td>14.4337 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>21.3930 ns</td><td>0.4570 ns</td><td>0.9439 ns</td><td>21.2226 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>24.6414 ns</td><td>0.5810 ns</td><td>1.6575 ns</td><td>24.2427 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>24.6491 ns</td><td>0.4937 ns</td><td>0.9972 ns</td><td>24.4348 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>3,478.0617 ns</td><td>65.6788 ns</td><td>149.5838 ns</td><td>3,450.3662 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>9,458.4904 ns</td><td>208.3826 ns</td><td>597.8889 ns</td><td>9,307.9445 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>27,806.5832 ns</td><td>704.6249 ns</td><td>2,077.6025 ns</td><td>27,493.1534 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>90,384.5923 ns</td><td>3,463.6120 ns</td><td>9,993.3126 ns</td><td>88,617.4011 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0000 ns</td><td>0.0000 ns</td><td>0.0000 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>5.0321 ns</td><td>0.2259 ns</td><td>0.6626 ns</td><td>4.8740 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>5.5561 ns</td><td>0.2084 ns</td><td>0.6113 ns</td><td>5.4859 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>6.3188 ns</td><td>0.1683 ns</td><td>0.4963 ns</td><td>6.2785 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>8.9291 ns</td><td>0.2493 ns</td><td>0.7350 ns</td><td>8.8480 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>15.2365 ns</td><td>0.3505 ns</td><td>1.0279 ns</td><td>15.0078 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>24.3105 ns</td><td>0.3886 ns</td><td>0.3635 ns</td><td>24.3210 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>25.3659 ns</td><td>0.8997 ns</td><td>2.5523 ns</td><td>24.8437 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>26.3175 ns</td><td>0.5359 ns</td><td>1.1420 ns</td><td>26.2766 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>38.3048 ns</td><td>0.7978 ns</td><td>2.3271 ns</td><td>37.7940 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>2,641.2160 ns</td><td>52.7157 ns</td><td>129.3124 ns</td><td>2,620.4657 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>6,717.1834 ns</td><td>129.3396 ns</td><td>181.3161 ns</td><td>6,738.5216 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>17,344.0375 ns</td><td>343.9448 ns</td><td>769.2818 ns</td><td>17,068.9468 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>72,232.5207 ns</td><td>1,912.8200 ns</td><td>5,363.7525 ns</td><td>71,045.8191 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>76.87 ns</td><td>1.880 ns</td><td>5.513 ns</td><td>75.50 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>82.40 ns</td><td>1.916 ns</td><td>5.649 ns</td><td>81.02 ns</td><td>1.08</td><td>0.10</td>
</tr><tr><td>Pure.DI</td><td>83.35 ns</td><td>2.217 ns</td><td>6.502 ns</td><td>81.38 ns</td><td>1.09</td><td>0.11</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>92.87 ns</td><td>2.169 ns</td><td>6.395 ns</td><td>91.25 ns</td><td>1.22</td><td>0.13</td>
</tr><tr><td>DryIoc</td><td>101.95 ns</td><td>2.434 ns</td><td>7.178 ns</td><td>100.63 ns</td><td>1.33</td><td>0.14</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>116.79 ns</td><td>3.314 ns</td><td>9.613 ns</td><td>114.23 ns</td><td>1.53</td><td>0.17</td>
</tr><tr><td>IoC.Container</td><td>125.21 ns</td><td>2.582 ns</td><td>7.531 ns</td><td>123.50 ns</td><td>1.64</td><td>0.16</td>
</tr><tr><td>LightInject</td><td>412.53 ns</td><td>8.201 ns</td><td>18.343 ns</td><td>406.62 ns</td><td>5.35</td><td>0.47</td>
</tr><tr><td>Unity</td><td>3,209.96 ns</td><td>63.584 ns</td><td>73.223 ns</td><td>3,206.25 ns</td><td>40.99</td><td>3.24</td>
</tr><tr><td>Autofac</td><td>7,810.48 ns</td><td>154.973 ns</td><td>413.655 ns</td><td>7,695.99 ns</td><td>101.90</td><td>8.96</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                    Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>90.14 ns</td><td>3.018 ns</td><td>8.898 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>99.19 ns</td><td>3.491 ns</td><td>10.293 ns</td><td>1.11</td><td>0.17</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>100.17 ns</td><td>3.280 ns</td><td>9.671 ns</td><td>1.12</td><td>0.16</td>
</tr><tr><td>Pure.DI</td><td>100.86 ns</td><td>2.944 ns</td><td>8.679 ns</td><td>1.13</td><td>0.16</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>107.44 ns</td><td>3.566 ns</td><td>10.515 ns</td><td>1.21</td><td>0.18</td>
</tr><tr><td>DryIoc</td><td>114.77 ns</td><td>4.064 ns</td><td>11.725 ns</td><td>1.29</td><td>0.17</td>
</tr><tr><td>IoC.Container</td><td>122.81 ns</td><td>4.125 ns</td><td>12.032 ns</td><td>1.37</td><td>0.19</td>
</tr><tr><td>LightInject</td><td>126.81 ns</td><td>2.961 ns</td><td>8.639 ns</td><td>1.42</td><td>0.17</td>
</tr><tr><td>Unity</td><td>4,540.45 ns</td><td>111.509 ns</td><td>325.276 ns</td><td>50.79</td><td>6.14</td>
</tr><tr><td>Autofac</td><td>10,841.80 ns</td><td>216.085 ns</td><td>576.775 ns</td><td>122.45</td><td>13.77</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>209.6 ns</td><td>4.26 ns</td><td>12.57 ns</td><td>207.2 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>216.9 ns</td><td>4.47 ns</td><td>13.05 ns</td><td>215.2 ns</td><td>1.04</td><td>0.08</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>220.9 ns</td><td>4.47 ns</td><td>12.82 ns</td><td>217.6 ns</td><td>1.06</td><td>0.08</td>
</tr><tr><td>Pure.DI</td><td>230.3 ns</td><td>5.51 ns</td><td>15.99 ns</td><td>226.5 ns</td><td>1.10</td><td>0.11</td>
</tr><tr><td>LightInject</td><td>242.4 ns</td><td>5.22 ns</td><td>15.40 ns</td><td>242.2 ns</td><td>1.16</td><td>0.09</td>
</tr><tr><td>DryIoc</td><td>245.5 ns</td><td>5.63 ns</td><td>16.51 ns</td><td>240.3 ns</td><td>1.18</td><td>0.10</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>258.5 ns</td><td>7.07 ns</td><td>20.84 ns</td><td>254.0 ns</td><td>1.24</td><td>0.13</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>291.6 ns</td><td>7.14 ns</td><td>21.06 ns</td><td>285.6 ns</td><td>1.40</td><td>0.13</td>
</tr><tr><td>IoC.Container</td><td>324.5 ns</td><td>10.37 ns</td><td>30.26 ns</td><td>316.6 ns</td><td>1.55</td><td>0.18</td>
</tr><tr><td>Unity</td><td>6,441.3 ns</td><td>133.83 ns</td><td>392.49 ns</td><td>6,337.7 ns</td><td>30.85</td><td>2.64</td>
</tr><tr><td>Autofac</td><td>11,450.2 ns</td><td>227.42 ns</td><td>544.88 ns</td><td>11,345.6 ns</td><td>54.75</td><td>4.41</td>
</tr></tbody></table>

</details>

<details>
<summary>Benchmarks environment</summary>

<pre><code>
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.2728/22H2/2022Update)
Intel Core i7-10850H CPU 2.70GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
</code></pre>

</details>
