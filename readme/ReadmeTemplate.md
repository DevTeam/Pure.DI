# Pure DI for .NET

[![NuGet](https://buildstats.info/nuget/Pure.DI?includePreReleases=true)](https://www.nuget.org/packages/Pure.DI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[<img src="http://teamcity.jetbrains.com/app/rest/builds/buildType:(id:OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType)/statusIcon"/>](http://teamcity.jetbrains.com/viewType.html?buildTypeId=OpenSourceProjects_DevTeam_PureDi_BuildAndTestBuildType&guest=1)

## Key features

Pure.DI is not a framework or library, but a source code generator. It generates a partial class to create object graphs in the Pure DI paradigm. To make this object graph accurate, it uses a set of hints that are checked at compile time. Since all the work is done at compile time, the efficient code is ready to use only at runtime. The generated code does not depend on .NET library calls or reflections and is efficient in terms of performance and memory consumption because, like regular code, it is subject to all optimizations and can be easily integrated into any application - without the use of delegates, additional method calls, boxes, type conversions, etc.

- [X] DI without any IoC/DI containers, frameworks, dependencies and hence no performance impact or side effects. 
  >_Pure.DI_ is actually a [.NET code generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview). It generates simple code as well as if you were doing it yourself: de facto it's just a bunch of nested constructor calls. And that code can be viewed at any time.
- [X] A predictable and verified dependency graph is built and validated on the fly while writing code.
  >All the logic of analyzing the graph of objects, constructors, and methods takes place at compile time. Thus, _Pure.DI_ notifies the developer about missing or circular dependencies, cases when some dependencies are not suitable for injection, etc. at the compilation time. There is no chance for the developer to get a program that crashes at runtime because of these errors. All this magic happens at the same time as you write your code, so you have instant feedback between the fact that you have made changes to your code and the fact that your code is already tested and ready to use.
- [X] Does not add any dependencies to other assemblies.
  >When using pure DI, no runtime dependencies are added to assemblies because only basic language constructs and nothing more are used.
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

*__Root__* - here [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) is the only place in the application where the composition of the object graph for the application takes place. Each instance is resolved by a strongly typed block of operators like operator [*__new__*](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator), which compile with all optimizations with minimal impact on performance or memory consumption. The generated _Composition_ class contains a property called _Root_, which is the root of the composition and allows an instance of type _Program_ to be resolved. There can be many such properties, per registered composition root. Therefore, each composition root must have its own name and will be represented by a separate property.

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

The full analog of this application with top-level statements can be found [here] (Samples/ShroedingersCatTopLevelStatements).

_Pure.DI_ creates efficient code in a pure DI paradigm, using only basic language constructs as if you were writing code by hand. This makes it possible to take full advantage of Dependency Injection everywhere and always, without any compromise!

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