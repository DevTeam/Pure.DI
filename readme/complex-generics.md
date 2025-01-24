#### Complex generics

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/ComplexGenericsScenario.cs)

Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ```IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }``` and its binding to the some implementation ```.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .RootArg<TT>("depArg")
    .Bind<IDependency<TT>>().To<Dependency<TT>>()
    .Bind<IDependency<TTS>>("value type")
        .As(Lifetime.Singleton)
        .To<DependencyStruct<TTS>>()
    .Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
        .To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()

    // Composition root
    .Root<Program<TT>>("GetRoot");

var composition = new Composition();
var program = composition.GetRoot<string>(depArg: "some value");
var service = program.Service;
service.ShouldBeOfType<Service<string, int, List<string>, Dictionary<string, int>>>();
service.Dependency1.ShouldBeOfType<Dependency<string>>();
service.Dependency2.ShouldBeOfType<DependencyStruct<int>>();

interface IDependency<T>;

class Dependency<T>(T value) : IDependency<T>;

readonly record struct DependencyStruct<T> : IDependency<T>
    where T : struct;

interface IService<T1, T2, TList, TDictionary>
    where T2 : struct
    where TList : IList<T1>
    where TDictionary : IDictionary<T1, T2>
{
    IDependency<T1> Dependency1 { get; }

    IDependency<T2> Dependency2 { get; }
}

class Service<T1, T2, TList, TDictionary>(
    IDependency<T1> dependency1,
    [Tag("value type")] IDependency<T2> dependency2)
    : IService<T1, T2, TList, TDictionary>
    where T2 : struct
    where TList : IList<T1>
    where TDictionary : IDictionary<T1, T2>
{
    public IDependency<T1> Dependency1 { get; } = dependency1;

    public IDependency<T2> Dependency2 { get; } = dependency2;
}

class Program<T>(IService<T, int, List<T>, Dictionary<T, int>> service)
    where T : notnull
{
    public IService<T, int, List<T>, Dictionary<T, int>> Service { get; } = service;
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private DependencyStruct<int> _singletonDependencyStruct51;
  private bool _singletonDependencyStruct51Created;

  [OrdinalAttribute(128)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Program<T3> GetRoot<T3>(T3 depArg)
    where T3: notnull
  {
    if (!_root._singletonDependencyStruct51Created)
    {
      using (_lock.EnterScope())
      {
        if (!_root._singletonDependencyStruct51Created)
        {
          _root._singletonDependencyStruct51 = new DependencyStruct<int>();
          Thread.MemoryBarrier();
          _root._singletonDependencyStruct51Created = true;
        }
      }
    }

    return new Program<T3>(new Service<T3, int, List<T3>, Dictionary<T3, int>>(new Dependency<T3>(depArg), _root._singletonDependencyStruct51));
  }
}
```

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	ServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ --|> IServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ
	DependencyᐸT3ᐳ --|> IDependencyᐸT3ᐳ
	DependencyStructᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : "value type" 
	Composition ..> ProgramᐸT3ᐳ : ProgramᐸT3ᐳ GetRootᐸT3ᐳ(T3 depArg)
	ProgramᐸT3ᐳ *--  ServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ : IServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ
	ServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	ServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ o-- "Singleton" DependencyStructᐸInt32ᐳ : "value type"  IDependencyᐸInt32ᐳ
	DependencyᐸT3ᐳ o-- T3 : Argument "depArg"
	namespace Pure.DI.UsageTests.Generics.ComplexGenericsScenario {
		class Composition {
		<<partial>>
		+ProgramᐸT3ᐳ GetRootᐸT3ᐳ(T3 depArg)
		}
		class DependencyStructᐸInt32ᐳ {
				<<struct>>
			+DependencyStruct()
		}
		class DependencyᐸT3ᐳ {
			+Dependency(T3 value)
		}
		class IDependencyᐸInt32ᐳ {
			<<interface>>
		}
		class IDependencyᐸT3ᐳ {
			<<interface>>
		}
		class IServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ {
			<<interface>>
		}
		class ProgramᐸT3ᐳ {
		}
		class ServiceᐸT3ˏInt32ˏListᐸT3ᐳˏDictionaryᐸT3ˏInt32ᐳᐳ {
			+Service(IDependencyᐸT3ᐳ dependency1, IDependencyᐸInt32ᐳ dependency2)
		}
	}
```

