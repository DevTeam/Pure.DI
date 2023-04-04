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

<pre><code>
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.2728/22H2/2022Update)
Intel Core i7-10850H CPU 2.70GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
</code></pre>

<details open>
<summary>Transient</summary>

<table>
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0003 ns</td><td>0.0014 ns</td><td>0.0014 ns</td><td>0.0000 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>0.4564 ns</td><td>0.0176 ns</td><td>0.0518 ns</td><td>0.4443 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>5.8993 ns</td><td>0.2273 ns</td><td>0.6701 ns</td><td>5.7136 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>6.0021 ns</td><td>0.2198 ns</td><td>0.6376 ns</td><td>5.9355 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>9.0995 ns</td><td>0.2405 ns</td><td>0.6295 ns</td><td>8.9616 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>12.3882 ns</td><td>0.2747 ns</td><td>0.7381 ns</td><td>12.3286 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>15.2185 ns</td><td>0.3838 ns</td><td>1.1135 ns</td><td>15.0544 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>20.7112 ns</td><td>0.4268 ns</td><td>1.0144 ns</td><td>20.5253 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>25.9913 ns</td><td>0.5478 ns</td><td>1.5089 ns</td><td>25.6916 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>26.2556 ns</td><td>0.5552 ns</td><td>0.8479 ns</td><td>26.2578 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>3,484.6997 ns</td><td>69.2992 ns</td><td>149.1739 ns</td><td>3,466.9380 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>9,948.6502 ns</td><td>198.9692 ns</td><td>574.0717 ns</td><td>9,868.5158 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>25,014.1281 ns</td><td>489.5747 ns</td><td>1,404.6816 ns</td><td>24,649.5209 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>96,008.5616 ns</td><td>3,132.9581 ns</td><td>9,039.3005 ns</td><td>93,386.5051 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Singleton</summary>

<table>
<thead><tr><th>                    Method</th><th>    Mean</th><th>  Error</th><th> StdDev</th><th>  Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>&#39;Hand Coded&#39;</td><td>0.0134 ns</td><td>0.0148 ns</td><td>0.0139 ns</td><td>0.0128 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>5.4185 ns</td><td>0.2592 ns</td><td>0.7642 ns</td><td>5.4074 ns</td><td> </td><td> </td>
</tr><tr><td>Pure.DI</td><td>6.6750 ns</td><td>0.2734 ns</td><td>0.8062 ns</td><td>6.5142 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>7.6283 ns</td><td>0.2453 ns</td><td>0.6998 ns</td><td>7.5603 ns</td><td> </td><td> </td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>10.5143 ns</td><td>0.3568 ns</td><td>1.0521 ns</td><td>10.1943 ns</td><td> </td><td> </td>
</tr><tr><td>IoC.Container</td><td>17.1709 ns</td><td>0.4807 ns</td><td>1.4024 ns</td><td>16.7716 ns</td><td> </td><td> </td>
</tr><tr><td>DryIoc</td><td>23.4310 ns</td><td>0.4970 ns</td><td>1.1812 ns</td><td>23.2243 ns</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>26.6139 ns</td><td>0.4883 ns</td><td>0.5014 ns</td><td>26.6371 ns</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>28.0878 ns</td><td>0.5918 ns</td><td>1.4739 ns</td><td>27.8425 ns</td><td> </td><td> </td>
</tr><tr><td>LightInject</td><td>39.1591 ns</td><td>0.9552 ns</td><td>2.6943 ns</td><td>38.5720 ns</td><td> </td><td> </td>
</tr><tr><td>Unity</td><td>2,803.4470 ns</td><td>52.7159 ns</td><td>148.6861 ns</td><td>2,783.5375 ns</td><td> </td><td> </td>
</tr><tr><td>Autofac</td><td>7,884.9173 ns</td><td>175.1691 ns</td><td>508.1974 ns</td><td>7,770.9152 ns</td><td> </td><td> </td>
</tr><tr><td>CastleWindsor</td><td>19,769.7655 ns</td><td>515.3359 ns</td><td>1,503.2589 ns</td><td>19,441.3513 ns</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>72,951.6858 ns</td><td>2,111.5661 ns</td><td>6,092.3510 ns</td><td>72,247.9675 ns</td><td> </td><td> </td>
</tr></tbody></table>

</details>

<details>
<summary>Func</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>CastleWindsor</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>93.02 ns</td><td>2.779 ns</td><td>8.108 ns</td><td>92.67 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>97.65 ns</td><td>2.882 ns</td><td>8.453 ns</td><td>96.21 ns</td><td>1.06</td><td>0.13</td>
</tr><tr><td>Pure.DI</td><td>98.73 ns</td><td>3.788 ns</td><td>11.109 ns</td><td>94.84 ns</td><td>1.07</td><td>0.15</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>111.79 ns</td><td>3.305 ns</td><td>9.590 ns</td><td>110.68 ns</td><td>1.21</td><td>0.17</td>
</tr><tr><td>DryIoc</td><td>114.99 ns</td><td>2.967 ns</td><td>8.654 ns</td><td>113.17 ns</td><td>1.25</td><td>0.15</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>138.08 ns</td><td>3.582 ns</td><td>10.161 ns</td><td>135.93 ns</td><td>1.50</td><td>0.15</td>
</tr><tr><td>IoC.Container</td><td>157.24 ns</td><td>5.699 ns</td><td>16.351 ns</td><td>153.22 ns</td><td>1.70</td><td>0.24</td>
</tr><tr><td>LightInject</td><td>505.35 ns</td><td>13.627 ns</td><td>38.878 ns</td><td>497.09 ns</td><td>5.47</td><td>0.62</td>
</tr><tr><td>Unity</td><td>3,723.72 ns</td><td>75.412 ns</td><td>218.783 ns</td><td>3,679.20 ns</td><td>40.27</td><td>3.93</td>
</tr><tr><td>Autofac</td><td>8,894.52 ns</td><td>174.956 ns</td><td>384.032 ns</td><td>8,836.11 ns</td><td>98.68</td><td>9.52</td>
</tr></tbody></table>

</details>

<details>
<summary>Array</summary>

<table>
<thead><tr><th>                    Method</th><th>  Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>CastleWindsor</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>87.50 ns</td><td>2.540 ns</td><td>7.450 ns</td><td>86.04 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>88.06 ns</td><td>1.800 ns</td><td>1.848 ns</td><td>87.41 ns</td><td>0.97</td><td>0.07</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>93.96 ns</td><td>2.914 ns</td><td>8.546 ns</td><td>92.00 ns</td><td>1.08</td><td>0.13</td>
</tr><tr><td>Pure.DI</td><td>96.92 ns</td><td>2.574 ns</td><td>7.467 ns</td><td>94.37 ns</td><td>1.12</td><td>0.12</td>
</tr><tr><td>LightInject</td><td>97.29 ns</td><td>2.806 ns</td><td>8.274 ns</td><td>94.26 ns</td><td>1.12</td><td>0.13</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>101.09 ns</td><td>2.752 ns</td><td>8.070 ns</td><td>100.55 ns</td><td>1.16</td><td>0.13</td>
</tr><tr><td>DryIoc</td><td>107.07 ns</td><td>2.667 ns</td><td>7.694 ns</td><td>105.91 ns</td><td>1.24</td><td>0.13</td>
</tr><tr><td>IoC.Container</td><td>109.96 ns</td><td>3.147 ns</td><td>9.078 ns</td><td>106.41 ns</td><td>1.27</td><td>0.15</td>
</tr><tr><td>Unity</td><td>4,257.55 ns</td><td>102.816 ns</td><td>296.648 ns</td><td>4,187.24 ns</td><td>49.23</td><td>5.82</td>
</tr><tr><td>Autofac</td><td>10,355.33 ns</td><td>205.123 ns</td><td>564.968 ns</td><td>10,207.63 ns</td><td>120.27</td><td>11.83</td>
</tr></tbody></table>

</details>

<details>
<summary>Enum</summary>

<table>
<thead><tr><th>                    Method</th><th> Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Ratio</th><th>RatioSD</th>
</tr>
</thead><tbody><tr><td>CastleWindsor</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>SimpleInjector</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>Ninject</td><td>NA</td><td>NA</td><td>NA</td><td>NA</td><td> </td><td> </td>
</tr><tr><td>&#39;Hand Coded&#39;</td><td>224.2 ns</td><td>10.07 ns</td><td>29.07 ns</td><td>219.4 ns</td><td>1.00</td><td>0.00</td>
</tr><tr><td>&#39;Pure.DI composition root&#39;</td><td>225.3 ns</td><td>6.13 ns</td><td>17.97 ns</td><td>220.5 ns</td><td>1.02</td><td>0.16</td>
</tr><tr><td>Pure.DI</td><td>225.8 ns</td><td>5.61 ns</td><td>16.45 ns</td><td>224.4 ns</td><td>1.03</td><td>0.17</td>
</tr><tr><td>&#39;Pure.DI non-generic&#39;</td><td>229.8 ns</td><td>5.30 ns</td><td>15.61 ns</td><td>225.4 ns</td><td>1.04</td><td>0.14</td>
</tr><tr><td>DryIoc</td><td>238.7 ns</td><td>4.80 ns</td><td>13.14 ns</td><td>236.9 ns</td><td>1.08</td><td>0.14</td>
</tr><tr><td>LightInject</td><td>244.7 ns</td><td>6.83 ns</td><td>19.48 ns</td><td>241.3 ns</td><td>1.11</td><td>0.15</td>
</tr><tr><td>MicrosoftDependencyInjection</td><td>252.1 ns</td><td>6.83 ns</td><td>19.92 ns</td><td>246.6 ns</td><td>1.14</td><td>0.18</td>
</tr><tr><td>&#39;IoC.Container composition root&#39;</td><td>315.6 ns</td><td>13.16 ns</td><td>38.80 ns</td><td>308.7 ns</td><td>1.42</td><td>0.24</td>
</tr><tr><td>IoC.Container</td><td>340.7 ns</td><td>12.47 ns</td><td>36.77 ns</td><td>343.2 ns</td><td>1.54</td><td>0.24</td>
</tr><tr><td>Unity</td><td>6,137.5 ns</td><td>121.28 ns</td><td>148.94 ns</td><td>6,142.9 ns</td><td>31.24</td><td>1.65</td>
</tr><tr><td>Autofac</td><td>10,891.7 ns</td><td>277.40 ns</td><td>782.40 ns</td><td>10,701.8 ns</td><td>49.57</td><td>7.90</td>
</tr></tbody></table>

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
