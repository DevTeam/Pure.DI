# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon"/>](http://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

## Key features

Pure.DI is not a framework or library, but a source code generator. It generates a partial class to create object graphs in the Pure DI paradigm. To make this object graph accurate, it uses a set of hints that are checked at compile time. Since all the work is done at compile time, you only have effective code ready to use at run time. The generated code is independent of .NET library calls or reflection and is efficient in terms of performance and memory consumption because, like normal code, it is subject to all optimizations and integrates seamlessly into any application - without using delegates, extra method calls, boxing, type conversions, etc.

- [X] DI without any IoC/DI containers, frameworks, dependencies and therefore without performance impact or side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It generates simple code as well as if you were doing it yourself: de facto just a bunch of nested constructors` calls. And you can see this code at any time.
- [X] A predictable and verifiable dependency graph is built and verified on the fly while you write the code.
  >All the logic for analyzing the graph of objects, constructors, methods happens at compile time. Thus, the _Pure.DI_ tool notifies the developer about missing or circular dependency, for cases when some dependencies are not suitable for injection, etc., at compile-time. Developers have no chance of getting a program that crashes at runtime due to these errors. All this magic happens simultaneously as the code is written, this way, you have instant feedback between the fact that you made some changes to your code and your code was already checked and ready for use.
- [X] Does not add any dependencies to other assemblies.
  >Using a pure DI approach, you don't add runtime dependencies to your assemblies.
- [X] Highest performance, including compiler and JIT optimizations.
  >All generated code runs as fast as your own, in pure DI style, including compile-time and run-time optimizations. As mentioned above, graph analysis doing at compile-time, but at run-time, there are just a bunch of nested constructors, and that's it.
- [X] It works everywhere.
  >Since a pure DI approach does not use any dependencies or the [.NET reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) at runtime, it does not prevent your code from working as expected on any platform: Full .NET Framework 2.0+, .NET Core, .NET, UWP/XBOX, .NET IoT, Xamarin, Native AOT, etc.
- [X] Ease of use.
  >The _Pure.DI_ API is very similar to the API of most IoC/DI libraries. And it was a deliberate decision: the main reason is that programmers do not need to learn a new API.
- [X] Ultra-fine tuning of generic types.
  >_Pure.DI_ offers special type markers instead of using open generic types. This allows you to more accurately build the object graph and take full advantage of generic types.
- [X] Supports basic .NET BCL types out of the box.
  >_Pure.DI_ already [supports](#base-class-library) many of [BCL types](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-libraries) like `Array`, `IEnumerable<T>`, `IList<T>`, `ISet<T>`, `Func<T>`, `ThreadLocal`, `Task<T>`, `MemoryPool<T>`, `ArrayPool<T>`, `ReadOnlyMemory<T>`, `Memory<T>`, `ReadOnlySpan<T>`, `Span<T>`, `IComparer<T>`, `IEqualityComparer<T>` and etc. without any extra effort.
- [X] Good for creating libraries or frameworks and where resource consumption is particularly critical.
  >High performance, zero memory consumption/preparation overhead and no dependencies make it an ideal assistant for building libraries and frameworks.

## Schrödinger's cat shows how it works [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](samples/ShroedingersCat)

### The reality is that

![Cat](readme/cat.png?raw=true)

### Let's create an abstraction

```c#
interface IBox<out T> { T Content { get; } }

interface ICat { State State { get; } }

enum State { Alive, Dead }
```

### Here's our implementation

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

It is important to note that our abstraction and implementation knows nothing about DI magic or any frameworks. Also, please note that an instance of type *__Lazy<>__* has been used here only as an example. However, using this type with non-trivial logic as a dependency is not recommended, so consider replacing it with some simple abstract type.

### Let's glue it all together

Add a package reference to

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)

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
partial class Composition
{
  // In fact, this code is never run, and the method can have any name or be a constructor, for example,
  // and can be in any part of the compiled code because this is just a hint to set up an object graph.
  // Here the setup is part of the generated class, just as an example.
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

Download the example project code

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