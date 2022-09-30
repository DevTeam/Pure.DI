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
  >Since a pure DI approach does not use any dependencies or the [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent your code from working as expected on any platform: Full .NET Framework 2.0+, .NET Core, .NET, UWP/XBOX, .NET IoT, Xamarin, etc.
- [X] Ease of use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And it was a deliberate decision: the main reason is that programmers do not need to learn a new API.
- [X] Ultra-fine tuning of generic types.
  >_Pure.DI_ offers special type markers instead of using open generic types. This allows you to more accurately build the object graph and take full advantage of generic types.
- [X] Supports basic .NET BCL types out of the box.
  >_Pure.DI_ already supports many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like Array, IEnumerable, IList, ISet, Func, ThreadLocal, etc. without any extra effort.

## Try it easy!

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

## Contents

- [How it works](#schrödingers-cat-shows-how-it-works)
- [API](#simple-and-powerful-api)
- [Requirements](#development-environment-requirements)
- [Supported frameworks](#supported-frameworks)
- [Samples](#samples)
  - [ASP.NET Core Blazor](#aspnet-core-blazor)
  - [WPF](#wpf)
- [Performance test](#performance-test)
- [Troubleshooting](#troubleshooting)
- [How to build this project](#how-to-build-this-project)
- [Other resources](#other-resources)
- [Usage scenarios](#usage-scenarios)

## [Schrödinger's cat](Samples/ShroedingersCat) shows how it works

### The reality is that

![Cat](Docs/Images/cat.png?raw=true)

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
static partial class Composer
{
  // Actually, this code never runs and the method might have any name or be a constructor for instance
  // because this is just a hint to set up an object graph.
  private static void Setup() => DI.Setup()
      // Models a random subatomic event that may or may not occur
      .Bind<Random>().As(Singleton).To<Random>()
      // Represents a quantum superposition of 2 states: Alive or Dead
      .Bind<State>().To(ctx => (State)ctx.Resolve<Random>().Next(2))
      // Represents schrodinger's cat
      .Bind<ICat>().To<ShroedingersCat>()
      // Represents a cardboard box with any content
      .Bind<IBox<TT>>().To<CardboardBox<TT>>()
      // Composition Root
      .Bind<Program>().To<Program>();
  }
}
```

The code above is just a chain of hints to define a dependency graph used to generate a static class *__Composer__* with method *__Resolve__*, which creates a composition root *__Program__* below. In fact, there is no reason to run this code, because it does nothing ant run-time, so it can be placed anywhere in the class (in methods,  in constructors, or in properties), and better where it will not be called. Its purpose is only to check the syntax of dependencies and to help in building a dependency graph at compile-time to generate static methods. In the example above, the name of the method ```Setup()``` was chosen arbitrarily, made private, and is not called anywhere. Only the name of the owner class matters, since it will be implicitly used to create a static partial class that will contain the logic for creating objects, in our case it is ```static partial class Composer```, although it can be defined explicitly.

> Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ``` interface IService<T1, T2, T3> where T3: IDictionary<T1, T2[]> { }``` and its binding to the some implementation ```Bind<IService<TT1, TT2, IDictionary<TT1, TT2[]>>>().To<Service<TT1, TT2, IDictionary<TT1, TT2[]>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.

### Time to open boxes!

```c#
class Program
{
  // Composition Root, a single place in an application
  // where the composition of the object graphs for an application take place
  public static void Main() => Composer.ResolveProgram().Run();

  private readonly IBox<ICat> _box;

  internal Program(IBox<ICat> box) => _box = box;

  private void Run() => Console.WriteLine(_box);
}
```

*__Program__* is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) here, a single place in an application where the composition of the object graphs for an application takes place. Each instance is resolved by a strongly-typed block of statements like the operator [*__new__*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator), which are compiling with all optimizations with minimal impact on performance or memory consumption. The creating of a composition root *__Program__*  is looking as ``````Composer.ResolveProgram()`````` here and the compiler replaces this statement with the set of constructor calls:

```c#
new Program(
  new CardboardBox<ICat>(
    new ShroedingersCat(
      new Lazy<State>(
        new Func<State>(
            () => (State)SingletonSystemRandom.Shared.Next(2)
        ),
        true // thread safe
      )
    )
  )
)
```

where ```SingletonSystemRandom``` is a private static class for the most efficient lazy-style ```Random``` singleton support, because the runtime loads the ```SingletonSystemRandom``` type and thread-safely initializes the static field ```Shared``` only the first time the composition code gets the value from ```SingletonSystemRandom.Shared```:

```c#
private static class SingletonSystemRandom
{
  static readonly Random Shared = new Random();
}
```

You can find a complete analogue of this application with top level statements [here](Samples/ShroedingersCatTopLevelStatements). For a top level statements application the name of generated composer is "Composer" by default if it was not override in the Setup call.

_Pure.DI_ works the same as calling a set of nested constructors, but allows dependency injection. And that's a reason to take full advantage of Dependency Injection everywhere, every time, without any compromise!

## Simple and powerful API.

```c#
// Starts DI configuration chain.
// This method contains a single optional argument to specify a custom DI type name to generate.
// By default, it is a name of an owner class.
DI.Setup("MyComposer")
  
  // This is a basic binding format:
  .Bind<IMyInterface>().To<MyImplementation>()

  // This option is also possible:
  .Bind<IMyInterface>().Bind<IMyInterface2>().To<MyImplementation>()

  // Determines a binding lifetime:
  .Bind<IMyInterface>().As(Lifetime.Singleton).To<MyImplementation>()
  
  // Determines tags for all dependency types of binding:
  .Bind<IMyInterface>().Tags(123).To<MyImplementation>()
  
  // Determines tags for the specific dependency type of binding:
  .Bind<IMyInterface>("MyImpl", 99).To<MyImplementation>()

  // Determines a binding implementation using a factory method,
  // it allows to create instance manually and to invoke required methods,
  // to initialize properties and etc.: 
  .Bind<IMyInterface>().To(
    ctx => new MyImplementation(
      ctx.Resolve<ISomeDependency1>(),
      "Some value",
      ctx.Resolve<ISomeDependency2>()))

  // Overrides a default lifetime (Transient by default):
  .Default(Lifetime.Singleton)

  // Determines a custom attribute overriding an injection type:
  .TypeAttribute<MyTypeAttribute>()
  
  // Determines a tag attribute overriding an injection tag:
  .TagAttribute<MyTagAttribute>()
  
  // Determines a custom attribute overriding an injection order:
  .OrderAttribute<MyOrderAttribute>()
  
  // Use some DI configuration as a base:
  .DependsOn(nameof(BasicComposer)) 
```

Predefined lifetimes:

- *__Transient__* - Creates a new object of the requested type every time.
- *__Singleton__* - Creates a singleton object first time you and then returns the same object.
- *__PerResolve__* - Similar to the Transient, but it reuses the same object in the recursive object graph. 
- *__ContainerSingleton__* - This lifetime is applicable for ASP.NET, specifies that a single instance of the service will be created
- *__Scoped__* - This lifetime is applicable for ASP.NET, specifies that a new instance of the service will be created for each scope

You can [add a lifetime](#custom-singleton-lifetime) yourself.

## Development environment requirements

- [.NET SDK 5.0.102 or newer](https://dotnet.microsoft.com/download/dotnet/5.0)
- [C# 4 or newer](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-40)

## Supported frameworks

- [.NET and .NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- [.NET Framework](https://docs.microsoft.com/en-us/dotnet/framework/) 2.0+
- [UWP/XBOX](https://docs.microsoft.com/en-us/windows/uwp/index)
- [.NET IoT](https://dotnet.microsoft.com/apps/iot)
- [Xamarin](https://dotnet.microsoft.com/apps/xamarin)
- [.NET Multi-platform App UI (MAUI)](https://docs.microsoft.com/en-us/dotnet/maui/)

## Samples

### [ASP.NET Core Blazor](Samples/BlazorServerApp)

![blazor](Docs/Images/blazor.png?raw=true)

When a targeting project is an ASP.NET project, a special extension method is generated automatically. This extension method could be used to integrate DI into a web application infrastructure. Pay attention to [this single statement](https://github.com/DevTeam/Pure.DI/blob/d1c4cdf3d6d7015f809cf7f9153d091a1d42dc34/Samples/BlazorServerApp/Startup.cs#L24)  that makes all magic.

### [WPF](Samples/WpfAppNetCore)

![wpf](Docs/Images/wpf.png?raw=true)

This sample demonstrates how to apply DI for a WPF application. The crucial class is [DataProvider](Samples/WpfAppNetCore/DataProvider.cs), which connects view and view models. Besides that, it provides two sets of models for [design-time](Samples/WpfAppNetCore/ClockDomainDesignTime.cs) and [running](Samples/WpfAppNetCore/ClockDomain.cs) modes.

## Performance test

### Graph of 27 transient instances

![Transient](http://teamcity.jetbrains.com/guestAuth/app/rest/builds/buildType:OpenSourceProjects_DevTeam_PureDi_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Transient-report.jpg)

### Graph of 20 transient instances and 1 singleton instance

![Singleton](http://teamcity.jetbrains.com/guestAuth/app/rest/builds/buildType:OpenSourceProjects_DevTeam_PureDi_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Singleton-report.jpg)

### Graph of 22 transient instances, including 3 Func to create 4 instances each time

![Func](http://teamcity.jetbrains.com/guestAuth/app/rest/builds/buildType:OpenSourceProjects_DevTeam_PureDi_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Func-report.jpg)

### Graph of 22 transient instances, including 3 arrays of 4 instances in each

![Array](http://teamcity.jetbrains.com/guestAuth/app/rest/builds/buildType:OpenSourceProjects_DevTeam_PureDi_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Array-report.jpg)

### Graph of 22 transient instances, including 3 enumerable of 4 instances in each

![Enum](http://teamcity.jetbrains.com/guestAuth/app/rest/builds/buildType:OpenSourceProjects_DevTeam_PureDi_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Enum-report.jpg)

_[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) was used to measure and analyze these results._

### Global Options

#### Changing the _Pure.DI_ API namespace

To change the default _Pure.DI_ namespace for the generated API to something else add a few lines to your project file:

```xml
<PropertyGroup>
  <PureDINamespace>MyNameSpace</PureDINamespace>
</PropertyGroup>
```

```xml
<ItemGroup>
  <CompilerVisibleProperty Include="PureDINamespace" />
</ItemGroup>
```

For instance, to use the default project namespace, you could specify the following lines:

```xml
<PropertyGroup>
  <PureDINamespace>$(RootNamespace)</PureDINamespace>
</PropertyGroup>
```

```xml
<ItemGroup>
  <CompilerVisibleProperty Include="PureDINamespace" />
</ItemGroup>
```

### Troubleshooting

See files emitted by the Pure.DI by adding these properties to your (*.csproj) project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

To get all generated source code and log, add a hint like ```// out=<path to the diagnostics directory >``` as a comment before calling the method ```DI.Setup()```, for instance:

```c#
// out=c:\Projects\MyDiagnostics
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

To change a log verbosity level use a hint like ```verbosity=<Verbosity level>```:

```c#
// out=c:\Projects\MyDiagnostics
// verbosity=Diagnostic
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

The list of verbosity levels:
- *__Quiet__*
- *__Minimal__*
- *__Normal__*
- *__Diagnostic__*

To debug a code generation, add a comment like `debug=true`:

```c#
// debug=true
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

To get a code-generation performance snapshot:

- install [JetBrains dotTrace command-line tool](https://www.jetbrains.com/help/profiler/Performance_Profiling__Profiling_Using_the_Command_Line.html#install-and-use-the-command-line-tool-as-a-net-core-tool):

```shell
dotnet tool install --global JetBrains.dotTrace.GlobalTools
```

- specify an output path like ```// out=<path to the diagnostics directory >```
- add a comment like ```trace=true```:

```c#
// out=c:\Projects\MyDiagnostics
// trace=true
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

### How to build this project

To run a build, tests and create a NuGet package, use the following command line from the solution directory:

```shell
dotnet tool restore
dotnet csi -p:target=Build Build
```

The NuGet package is created here `./Pure.DI/bin/Release`

To run benchmarks:

```shell
dotnet tool restore
dotnet csi -p:target=Benchmark Build
```

Benchmark results are placed here `./BenchmarkDotNet.Artifacts/results`

### Other resources

* [Project templates](https://github.com/DevTeam/Pure.DI/wiki/Project-templates) - project templates for _dotnet new_ command
* [Schrödinger's cat sample](Samples/ShroedingersCat) - simple console application
* [Top level statements sample](Samples/ShroedingersCatTopLevelStatements) - simple console top level statements application
* [C# script tool](https://github.com/JetBrains/teamcity-csharp-interactive/blob/master/TeamCity.CSharpInteractive/Composer.cs) - JetBrain TeamCity interactive tool for running C# scripts
* [MSBuild logger](https://github.com/JetBrains/teamcity-msbuild-logger/blob/master/TeamCity.MSBuild.Logger/Composer.cs) - Provides the JetBrain TeamCity integration with Microsoft MSBuild.
* [Performance comparison](https://danielpalme.github.io/IocPerformance/) - performance comparison of the most popular .NET DI/IoC containers