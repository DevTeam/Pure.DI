# Pure DI for .NET

[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE) [<img src="http://tcavs2015.cloudapp.net/app/rest/builds/buildType:(id:DevTeam_PureDI_Build)/statusIcon"/>](http://tcavs2015.cloudapp.net/viewType.html?buildTypeId=DevTeam_PureDI_Build&guest=1)

<img src="https://github.com/DevTeam/Pure.DI/blob/master/Docs/Images/demo.gif"/>

#### Base concepts:

- DI without any IoC/DI containers, frameworks, dependencies, and thus without any performance impacts
- High performance with all .NET compiler/JIT optimizations
- Ultra-fine tuning of generic types
- A predictable dependency graph which are building on the fly while you are writing your code

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

_It is important to note that our abstraction and our implementation do not know anything about any IoC containers at all._

### Let's glue all together

Just add the package reference to [Pure.DI](https://www.nuget.org/packages/Pure.DI). It ships entirely as NuGet packages.

_Using NuGet packages allows you to optimize your application to include only the necessary dependencies._

- Package Manager

  ```
  Install-Package Pure.DI
  ```
  
- .NET CLI
  
  ```
  dotnet add package Pure.DI
  ```

Declare the required dependencies in a dedicated class *__Glue__*. It is possible to do this anywhere in your code, but putting this information in one place is often the better solution and helps keep your code more organized.

```csharp
static partial class Composer
{
  // Models a random subatomic event that may or may not occur
  private static readonly Random Indeterminacy = new();

  static Composer()
  {
    DI.Setup()
      // .NET BCL types
      .Bind<Func<TT>>().To(ctx => new Func<TT>(ctx.Resolve<TT>))
      .Bind<Lazy<TT>>().To<Lazy<TT>>()
      // Represents a quantum superposition of 2 states: Alive or Dead
      .Bind<State>().To(ctx => (State)Indeterminacy.Next(2))
      // Represents schrodinger's cat
      .Bind<ICat>().To<ShroedingersCat>()
      // Represents a cardboard box with any content
      .Bind<IBox<TT>>().To<CardboardBox<TT>>()
      // Composition Root
      .Bind<Program>().As(Singleton).To<Program>();
  }
}
```

_Defining generic type arguments using special marker types like [*__TT__*](#generic-autowiring-) in the sample above is one of the distinguishing features of this library. So there is an easy way to bind complex generic types with nested generic types and with any type constraints._

### Time to open boxes!

```csharp
class Program
{
  // Composition Root, a single place in an application where the composition of the object graphs for an application take place
  public static void Main() => Composer.Resolve<Program>().Run();

  private readonly IBox<ICat> _box;

  internal Program(IBox<ICat> box) => _box = box;

  private void Run() => Console.WriteLine(_box);
}
```

This is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) - a single place in an application where the composition of the object graphs for an application take place. Each instance is resolved by a strongly-typed block of statements like the operator new which is compiled on the fly from the corresponding expression tree with minimal impact on performance or memory consumption. For instance, the getting of a box looks like:

```csharp
// Models a random subatomic event that may or may not occur
Random Indeterminacy = new();

new Sample.Program(
  new Sample.CardboardBox<Sample.ICat>(
    new Sample.ShroedingersCat(
      new Lazy<Sample.State>(
        new Func<Sample.State>(
          () => Indeterminacy.Next(2)))));
```

It allows you to take full advantage of dependency injection everywhere and every time without any compromises!

## NuGet packages

|     | packages |
| --- | --- |
| Container | [![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI) |

