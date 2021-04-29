# Pure DI for .NET

[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE) [<img src="http://tcavs2015.cloudapp.net/app/rest/builds/buildType:(id:DevTeam_PureDI_Build)/statusIcon"/>](http://tcavs2015.cloudapp.net/viewType.html?buildTypeId=DevTeam_PureDI_Build&guest=1)

<img src="https://github.com/DevTeam/Pure.DI/blob/master/Docs/Images/demo.gif"/>

#### Base concepts:

- DI without any IoC/DI containers, frameworks, dependencies, and thus without any performance impacts and side-effects
- A predictable and validated dependencies graph which is building and validating on the fly while you are writing your code
- Does not add dependencies to any other assemblies
- High performance with all .NET compiler/JIT optimizations
- Easy to use
- Ultra-fine tuning of generic types
- Supports major .NET BCL types from the box

## [Schrödinger's cat](Samples/ShroedingersCat) shows how it works

### The reality is that

![Cat](https://github.com/DevTeam/IoCContainer/blob/master/Docs/Images/cat.jpg?raw=true)

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

_It is important to note that our abstraction and our implementation do not know anything about any IoC/DI containers at all._

### Let's glue all together

Just add a package reference to [Pure.DI](https://www.nuget.org/packages/Pure.DI). Using NuGet packages allows you to optimize your application to include only the necessary dependencies.

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
static partial class Glue
{
  // Models a random subatomic event that may or may not occur
  private static readonly Random Indeterminacy = new();

  static Glue()
  {
    DI.Setup()
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

_Defining generic type arguments using special marker types like [*__TT__*](Pure.DI/GenericTypeArguments.cs) in the sample above is one of the distinguishing features of this library. So there is an easy way to bind complex generic types with nested generic types and with any type constraints._

### Time to open boxes!

```csharp
class Program
{
  // Composition Root, a single place in an application where the composition of the object graphs for an application take place
  public static void Main() => Glue.Resolve<Program>().Run();

  private readonly IBox<ICat> _box;

  internal Program(IBox<ICat> box) => _box = box;

  private void Run() => Console.WriteLine(_box);
}
```

_Program_ is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) here, a single place in an application where the composition of the object graphs for an application take place. To have an ability create multiple instances or to do it on demand you could use *__Func<>__* with required type specified. Each instance is resolved by a strongly-typed block of statements like the operator new which are compiled with all optimizations with minimal impact on performance or memory consumption. For instance, the creating of a composition root *__Program__* looks like this:
```csharp
Random Indeterminacy = new();

new Program(
  new CardboardBox<ICat>(
    new ShroedingersCat(
      new Lazy<State>(
        new Func<State>(
          () => (State)Indeterminacy.Next(2)))));
```

Take full advantage of Dependency Injection everywhere and every time without any compromise.

## NuGet package

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Development environment requirements

- [.NET 5.0.102+](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Visual Studio 16.8+](https://visualstudio.microsoft.com/vs)
- [C# 4.0+](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-40)

## Supported frameworks

- .NET 5.0+
- [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- .NET Framework 3.5+
- [UWP](https://docs.microsoft.com/en-us/windows/uwp/index) 10+

## ASP.NET Support

When a targeting project is an ASP.NET project, a special extension method is generated automatically. This extension method could be used to integrate DI into a web application infrastructure. Pay attention to [this single statement](https://github.com/DevTeam/Pure.DI/blob/d1c4cdf3d6d7015f809cf7f9153d091a1d42dc34/Samples/BlazorServerApp/Startup.cs#L24)  that makes all magic. For more details, please take a look at this [sample](https://github.com/DevTeam/Pure.DI/tree/master/Samples/BlazorServerApp).
