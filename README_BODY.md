# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://tcavs2015.cloudapp.net/app/rest/builds/buildType:(id:DevTeam_Pure_Di_BuildAndTestBuildType)/statusIcon"/>](http://tcavs2015.cloudapp.net/viewType.html?buildTypeId=DevTeam_Pure_Di_BuildAndTestBuildType&guest=1)

<img src="Docs/Images/demo.gif"/>

## Key features

_Pure.DI_ is __NOT__ a framework or library, but a tool. It generates static method code for creating an object graph in the paradigm of pure DI using a set of hints. The generated code does not rely on library calls or .NET reflection, so it is efficient.

- [X] DI without any IoC/DI containers, frameworks, dependencies and therefore without any performance impact and side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It generates simple code as if you were doing it yourself: de facto just a bunch of constructors` calls. And you can see this code at any time.
- [X] A predictable and verified dependency graph is built and verified on the fly while you write your code.
  >All the logic for analyzing the graph of objects, constructors, methods happens at compile time. Thus, the _Pure.DI_ tool notifies the developer about missing dependencies, circular dependencies, for cases when some dependency is not suitable for injection, etc. at compile-time. Developers have no chance of getting a program that crashes at runtime due to these errors. All this magic happens at the same time as the code is written. This way, you have instant feedback between the fact that you made some changes to your code and _Pure.DI_ checked your code.
- [X] Does not add dependencies to other assemblies.
  >Using a pure DI approach, you don't add any runtime dependencies to your assemblies.
- [X] High performance, including C# and JIT compilers optimizations.
  >All generated code runs as fast as your own, in pure DI style, including compile-time and run-time optimizations. As mentioned above, graph analysis is done at compile time. At runtime, there is just a bunch of nested compiled constructors and that's it.
- [X] Works everywhere.
  >Since a pure DI approach does not use any dependencies or the [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent your code from working as expected on any platform: .NET Framework, .NET Core, UWP / XBOX, .NET IoT, Xamarin, etc.
- [X] Ease of Use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And it was a deliberate decision: the main reason is that programmers do not need to learn a new API.
- [X] Ultra-fine tuning of generic types.
  >_Pure.DI_ offers special type markers instead of using open generic types. This allows you to more accurately build the object graph and take full advantage of generic types.
- [X] Supports basic .NET BCL types out of the box.
  >_Pure.DI_ already supports many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like Array, IEnumerable, IList, ISet, Func, ThreadLocal, etc. without any extra effort.

## Contents

- [How it works](#schrödingers-cat-shows-how-it-works)
- [API](#simple-and-powerful-api)
- [Requirements](#development-environment-requirements)
- [Supported frameworks](#supported-frameworks)
- [Project templates](#project-templates)
- [Samples](#samples)
  - [ASP.NET Core Blazor](#aspnet-core-blazor) 
  - [WPF](#wpf)
- [Performance test](#performance-test)
- [Troubleshooting](#troubleshooting)
- [Other resources](#other-resources)
- [Usage scenarios](#usage-scenarios)

## [Schrödinger's cat](Samples/ShroedingersCat) shows how it works

### The reality is that

![Cat](Docs/Images/cat.png?raw=true)

### Let's create an abstraction

```csharp
interface IBox<out T> { T Content { get; } }

interface ICat { State State { get; } }

enum State { Alive, Dead }
```

### Here is our implementation

```csharp
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

It is important to note that our abstraction and implementation do not know anything about DI magic or any frameworks.

### Let's glue all together

Add a package reference to:

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

- Package Manager

  ```
  Install-Package Pure.DI
  ```
  
- .NET CLI
  
  ```
  dotnet add package Pure.DI
  ```

Declare required dependencies in a class like:

```csharp
static partial class Composer
{
  // Models a random subatomic event that may or may not occur
  private static readonly Random Indeterminacy = new();

  // Actually this code never runs, this is just a hint to set up an object graph
  private static void Setup() => DI.Setup()
      // Represents a quantum superposition of 2 states: Alive or Dead
      .Bind<State>().To(_ => (State)Indeterminacy.Next(2))
      // Represents schrodinger's cat
      .Bind<ICat>().To<ShroedingersCat>()
      // Represents a cardboard box with any content
      .Bind<IBox<TT>>().To<CardboardBox<TT>>()
      // Composition Root
      .Bind<Program>().As(Singleton).To<Program>();
  }
}
```

The code above is actually a chain of hints to generate a static class *__Composer__* with method *__Resolve__*, which creates a composition root *__Program__* below.

> Defining generic type arguments using special marker types like *__TT__* in the sample above is one of the distinguishing features of this library. So there is an easy way to bind complex generic types with nested generic types and with any type constraints.

### Time to open boxes!

```csharp
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

*__Program__* is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) here, a single place in an application where the composition of the object graphs for an application take place. To have an ability create multiple instances or to do it on demand you could use *__Func<>__* with required type specified. Each instance is resolved by a strongly-typed block of statements like the operator [*__new__*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator) which are compiled with all optimizations with minimal impact on performance or memory consumption. For instance, the creating of a composition root *__Program__* looks like this:
```csharp
Random Indeterminacy = new();

new Program(
  new CardboardBox<ICat>(
    new ShroedingersCat(
      new Lazy<State>(
        new Func<State>(
          () => (State)Indeterminacy.Next(2)))));
```

Take full advantage of Dependency Injection everywhere and every time without any compromises!

## Simple and powerful API.

```csharp
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

- [.NET SDK 5.0.102+](https://dotnet.microsoft.com/download/dotnet/5.0)
- [C# v.4 or newer](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-40)

## Supported frameworks

- [.NET and .NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- [.NET Framework](https://docs.microsoft.com/en-us/dotnet/framework/) 3.5+
- [UWP/XBOX](https://docs.microsoft.com/en-us/windows/uwp/index)
- [.NET IoT](https://dotnet.microsoft.com/apps/iot)
- [Xamarin](https://dotnet.microsoft.com/apps/xamarin)
- [.NET Multi-platform App UI (MAUI)](https://docs.microsoft.com/en-us/dotnet/maui/)

## Project templates

Run the following command to install [Pure.DI templates](https://www.nuget.org/packages/Pure.DI.Templates) for _dotnet new_ command:

```dotnet new -i Pure.DI.Templates```

To create a new C# DI-based console project from the template, run:

```dotnet new di```

After that, you can run the created application:

```dotnet run```

Please see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates) for more details.

## Samples

### [ASP.NET Core Blazor](Samples/BlazorServerApp)

![blazor](Docs/Images/blazor.png?raw=true)

When a targeting project is an ASP.NET project, a special extension method is generated automatically. This extension method could be used to integrate DI into a web application infrastructure. Pay attention to [this single statement](https://github.com/DevTeam/Pure.DI/blob/d1c4cdf3d6d7015f809cf7f9153d091a1d42dc34/Samples/BlazorServerApp/Startup.cs#L24)  that makes all magic.

### [WPF](Samples/WpfAppNetCore)

![wpf](Docs/Images/wpf.png?raw=true)

This sample demonstrates how to apply DI for a WPF application. The crucial class is [DataProvider](Samples/WpfAppNetCore/DataProvider.cs), which connects view and view models. Besides that, it provides two sets of models for [design-time](Samples/WpfAppNetCore/ClockDomainDesignTime.cs) and [running](Samples/WpfAppNetCore/ClockDomain.cs) modes.

For details please see [this sample](IoC.Tests/UsageScenarios/Interception.cs).

## Performance test

### Graph of 27 transient instances

![Transient](http://tcavs2015.cloudapp.net/guestAuth/app/rest/builds/buildType:DevTeam_Pure_Di_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Transient-report.jpg)

### Graph of 20 transient instances and 1 singleton instance

![Singleton](http://tcavs2015.cloudapp.net/guestAuth/app/rest/builds/buildType:DevTeam_Pure_Di_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Singleton-report.jpg)

### Graph of 22 transient instances, including 3 Func to create 4 instances each time

![Func](http://tcavs2015.cloudapp.net/guestAuth/app/rest/builds/buildType:DevTeam_Pure_Di_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Func-report.jpg)

### Graph of 22 transient instances, including 3 arrays of 4 instances in each

![Array](http://tcavs2015.cloudapp.net/guestAuth/app/rest/builds/buildType:DevTeam_Pure_Di_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Array-report.jpg)

### Graph of 22 transient instances, including 3 enumerable of 4 instances in each

![Enum](http://tcavs2015.cloudapp.net/guestAuth/app/rest/builds/buildType:DevTeam_Pure_Di_BenchmarkBuildType,pinned:true,status:SUCCESS/artifacts/content/Pure.DI.Benchmark.Benchmarks.Enum-report.jpg)

- __new__ - the baseline method when an object's graph was constructed by operators _new_ only, without any tricks
- __Mean__ - arithmetic mean of the root instances resolved per nanosecond
- __Error__ - half of 99.9% confidence interval
- __StdDev__ - standard deviation of all measurements
- __Median__ - value separating the higher half of all measurements (50th percentile)
- __1 ns__ - 1 Nanosecond (0.000000001 sec)

_[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) was used to measure and analyze these results._

### Troubleshooting

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
- Quiet
- Minimal
- Normal
- Diagnostic

To debug a code generation, add a hint like ```debug=true```:
```c#
// debug=true
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

To get a code-generation performance snapshot:
- install [JetBrains dotTrace command-line tool](https://www.jetbrains.com/help/profiler/Performance_Profiling__Profiling_Using_the_Command_Line.html#install-and-use-the-command-line-tool-as-a-net-core-tool):
```
dotnet tool install --global JetBrains.dotTrace.GlobalTools
```
- specify an output path like ```// out=<path to the diagnostics directory >```
- add a hint like ```debug=true```:
```c#
// out=c:\Projects\MyDiagnostics
// trace=true
DI.Setup()
  .Bind<IDependency>().To<Dependency>();
```

### Other resources

* [Project templates](https://github.com/DevTeam/Pure.DI/wiki/Project-templates) - project templates for _dotnet new_ command
* [Schrödinger's cat](Samples/ShroedingersCat) - simple console application
* [C# script tool](https://github.com/JetBrains/teamcity-csharp-interactive/blob/master/TeamCity.CSharpInteractive/Composer.cs) - JetBrain TeamCity interactive tool for running C# scripts
* [MSBuild logger](https://github.com/JetBrains/teamcity-msbuild-logger/blob/master/TeamCity.MSBuild.Logger/Composer.cs) - Provides the JetBrain TeamCity integration with Microsoft MSBuild.
* [Performance comparison](https://danielpalme.github.io/IocPerformance/) - performance comparison of the most popular .NET DI/IoC containers
