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
### Hints
- [Resolve Hint](readme/Examples.md#resolve-hint)
- [ThreadSafe Hint](readme/Examples.md#threadsafe-hint)
- [OnDependencyInjection Hint](readme/Examples.md#ondependencyinjection-hint)
- [OnCannotResolve Hint](readme/Examples.md#oncannotresolve-hint)
- [OnInstanceCreation Hint](readme/Examples.md#oninstancecreation-hint)
- [ToString Hint](readme/Examples.md#tostring-hint)
## Composition class

For each generated class, hereinafter referred to as _composition_, the setup must be done. It starts with the `Setup(...)` method, for example:

```c#
DI.Setup("Composition")
    ...;
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

To be able to quickly and conveniently create an object graph, a set of properties is generated. The type of the property is the type of the root object created by the composition. Accordingly, each access to the property leads to the creation of a composition with the root element of this type.

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

This is [recommended way](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) to create a composition root. A composition class can contain any number of roots.

### Private Roots

The composition has properties for each potential root that are used in those _Resolve_ methods. For example:

```c#
private IService Root2PropABB3D0
{
    get { ... }
}
```

These properties have a random name and a private accessor and cannot be used directly from code. Don't try to use them.

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

These methods are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach when the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) way to create a composition root. To control the generation of these methods, see the _Resolve_ hint.

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
| [OnDependencyInjectionLifetimeRegularExpression](#OnDependencyInjectionLifetimeRegularExpression-Hint)                             | .+            |
| [OnCannotResolve](#OnCannotResolve-Hint)                                                                                           | Off           |
| [OnCannotResolveContractTypeNameRegularExpression](#OnCannotResolveContractTypeNameRegularExpression-Hint)                         | .+            |
| [OnCannotResolveTagRegularExpression](#OnCannotResolveTagRegularExpression-Hint)                                                   | .+            |
| [OnCannotResolveLifetimeRegularExpression](#OnCannotResolveLifetimeRegularExpression-Hint)                                         | .+            |
| [ToString](#ToString-Hint)                                                                                                         | Off           |
| [ThreadSafe](#ThreadSafe-Hint)                                                                                                     | On            |

### Resolve Hint

Determine whether to generate [_Resolve_ methods](#resolve-methods). By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no [private composition roots](#Private-Roots) will be generated in this case. The composition will be tiny and will only have [public roots](#Public-Roots). When the _Resolve_ hint is disabled, only the public root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.

### OnInstanceCreation Hint

Determine whether to generate partial _OnInstanceCreation_ method. This partial method is generated by default, has no body, and can be overridden as needed. If the body is not defined, then the compiler will cut out its calls. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnInstanceCreation<T>(ref T value, object? tag, object lifetime)            
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

Determine whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden on creation.

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
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0000 ns</td><td>0.0000 ns</td><td>0.0000 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>0.4303 ns</td><td>0.0142 ns</td><td>0.0330 ns</td><td>0.4152 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>4.5776 ns</td><td>0.1244 ns</td><td>0.1900 ns</td><td>4.5021 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>6.7533 ns</td><td>0.1651 ns</td><td>0.3259 ns</td><td>6.6503 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>7.7167 ns</td><td>0.1204 ns</td><td>0.1067 ns</td><td>7.6917 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>10.9557 ns</td><td>0.3219 ns</td><td>0.9289 ns</td><td>10.6802 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>12.6151 ns</td><td>0.2089 ns</td><td>0.2145 ns</td><td>12.6279 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>18.7482 ns</td><td>0.1502 ns</td><td>0.1332 ns</td><td>18.7593 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>21.6415 ns</td><td>0.4536 ns</td><td>0.7579 ns</td><td>21.3362 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>22.1023 ns</td><td>0.2293 ns</td><td>0.1915 ns</td><td>22.0782 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>3,183.1330 ns</td><td>63.1780 ns</td><td>108.9789 ns</td><td>3,163.4781 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>8,200.5722 ns</td><td>124.5648 ns</td><td>97.2520 ns</td><td>8,233.8547 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>22,880.6503 ns</td><td>457.2682 ns</td><td>812.7938 ns</td><td>22,535.7346 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>77,290.9201 ns</td><td>2,216.3830 ns</td><td>6,287.5097 ns</td><td>76,955.4077 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0195 ns</td><td>0.0229 ns</td><td>0.0235 ns</td><td>0.0063 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>3.8203 ns</td><td>0.0354 ns</td><td>0.0296 ns</td><td>3.8193 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>4.9871 ns</td><td>0.1351 ns</td><td>0.2022 ns</td><td>4.9011 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>6.0158 ns</td><td>0.1227 ns</td><td>0.1148 ns</td><td>6.0008 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>9.0370 ns</td><td>0.2364 ns</td><td>0.4935 ns</td><td>8.9314 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>14.5328 ns</td><td>0.3201 ns</td><td>0.6319 ns</td><td>14.3176 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>19.6103 ns</td><td>0.3828 ns</td><td>0.3393 ns</td><td>19.5617 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>21.8840 ns</td><td>0.4605 ns</td><td>0.4729 ns</td><td>21.8218 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>23.3222 ns</td><td>0.4075 ns</td><td>0.4529 ns</td><td>23.2933 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>32.3530 ns</td><td>0.6412 ns</td><td>1.2506 ns</td><td>31.8038 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>2,508.6886 ns</td><td>49.2618 ns</td><td>82.3053 ns</td><td>2,489.2700 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>6,745.9371 ns</td><td>134.0016 ns</td><td>267.6157 ns</td><td>6,637.3131 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>17,408.9145 ns</td><td>262.2540 ns</td><td>218.9940 ns</td><td>17,312.9379 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>60,594.4595 ns</td><td>1,249.6474 ns</td><td>3,585.4726 ns</td><td>59,927.0508 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>75.24 ns</td><td>1.734 ns</td><td>5.057 ns</td><td>72.82 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>Pure.DI</td><td>79.78 ns</td><td>1.620 ns</td><td>1.990 ns</td><td>79.14 ns</td><td>1.08</td><td>0.05</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>79.82 ns</td><td>1.656 ns</td><td>4.724 ns</td><td>77.68 ns</td><td>1.07</td><td>0.10</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>87.24 ns</td><td>1.804 ns</td><td>5.292 ns</td><td>84.71 ns</td><td>1.16</td><td>0.10</td>
</tr><tr><td>DryIoc</td><td>94.14 ns</td><td>1.798 ns</td><td>1.501 ns</td><td>94.11 ns</td><td>1.26</td><td>0.05</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>109.83 ns</td><td>2.212 ns</td><td>4.468 ns</td><td>107.90 ns</td><td>1.46</td><td>0.12</td>
</tr><tr><td>IoC.Container</td><td>125.72 ns</td><td>2.535 ns</td><td>2.371 ns</td><td>125.12 ns</td><td>1.70</td><td>0.07</td>
</tr><tr><td>LightInject</td><td>415.89 ns</td><td>8.252 ns</td><td>20.242 ns</td><td>408.93 ns</td><td>5.55</td><td>0.48</td>
</tr><tr><td>Unity</td><td>3,253.41 ns</td><td>64.917 ns</td><td>128.140 ns</td><td>3,221.60 ns</td><td>43.29</td><td>3.58</td>
</tr><tr><td>Autofac</td><td>7,941.42 ns</td><td>158.057 ns</td><td>366.321 ns</td><td>7,875.67 ns</td><td>106.52</td><td>8.71</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                    Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>84.83 ns</td><td>2.587 ns</td><td>7.586 ns</td><td>81.75 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>89.55 ns</td><td>2.367 ns</td><td>6.942 ns</td><td>87.56 ns</td><td>1.06</td><td>0.12</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>93.20 ns</td><td>2.723 ns</td><td>7.987 ns</td><td>91.12 ns</td><td>1.11</td><td>0.13</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>97.34 ns</td><td>1.940 ns</td><td>2.235 ns</td><td>97.35 ns</td><td>1.13</td><td>0.11</td>
</tr><tr><td>Pure.DI</td><td>98.16 ns</td><td>2.980 ns</td><td>8.693 ns</td><td>94.92 ns</td><td>1.17</td><td>0.15</td>
</tr><tr><td>LightInject</td><td>98.69 ns</td><td>3.073 ns</td><td>8.865 ns</td><td>95.45 ns</td><td>1.17</td><td>0.15</td>
</tr><tr><td>IoC.Container</td><td>108.70 ns</td><td>3.237 ns</td><td>9.543 ns</td><td>105.17 ns</td><td>1.29</td><td>0.15</td>
</tr><tr><td>DryIoc</td><td>135.24 ns</td><td>6.780 ns</td><td>19.883 ns</td><td>134.38 ns</td><td>1.61</td><td>0.30</td>
</tr><tr><td>Unity</td><td>4,405.14 ns</td><td>96.115 ns</td><td>280.372 ns</td><td>4,360.28 ns</td><td>52.25</td><td>5.34</td>
</tr><tr><td>Autofac</td><td>10,752.87 ns</td><td>213.598 ns</td><td>591.880 ns</td><td>10,661.81 ns</td><td>127.12</td><td>12.40</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>191.9 ns</td><td>3.85 ns</td><td>8.36 ns</td><td>188.7 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>204.9 ns</td><td>4.16 ns</td><td>10.06 ns</td><td>201.2 ns</td><td>1.08</td><td>0.08</td>
</tr><tr><td>Pure.DI</td><td>218.7 ns</td><td>4.44 ns</td><td>12.81 ns</td><td>215.6 ns</td><td>1.14</td><td>0.10</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>224.5 ns</td><td>4.84 ns</td><td>13.97 ns</td><td>222.5 ns</td><td>1.18</td><td>0.07</td>
</tr><tr><td>LightInject</td><td>226.0 ns</td><td>4.98 ns</td><td>14.69 ns</td><td>221.8 ns</td><td>1.17</td><td>0.10</td>
</tr><tr><td>DryIoc</td><td>234.8 ns</td><td>4.94 ns</td><td>14.42 ns</td><td>231.2 ns</td><td>1.23</td><td>0.09</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>240.1 ns</td><td>4.79 ns</td><td>11.01 ns</td><td>238.2 ns</td><td>1.25</td><td>0.07</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>287.5 ns</td><td>7.64 ns</td><td>22.29 ns</td><td>284.1 ns</td><td>1.52</td><td>0.14</td>
</tr><tr><td>IoC.Container</td><td>302.5 ns</td><td>6.22 ns</td><td>18.14 ns</td><td>298.9 ns</td><td>1.58</td><td>0.12</td>
</tr><tr><td>Unity</td><td>6,219.7 ns</td><td>123.50 ns</td><td>342.21 ns</td><td>6,138.6 ns</td><td>32.54</td><td>2.20</td>
</tr><tr><td>Autofac</td><td>10,359.5 ns</td><td>206.85 ns</td><td>555.69 ns</td><td>10,206.7 ns</td><td>53.69</td><td>4.29</td>
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
