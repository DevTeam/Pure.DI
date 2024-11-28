#### Complex generics

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/ComplexGenericsScenario.cs)

Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ```IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }``` and its binding to the some implementation ```.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.


```c#
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
```

It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private DependencyStruct<int> _singletonDependencyStruct51;
  private bool _singletonDependencyStruct51Created;

  [OrdinalAttribute(10)]
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
  public Program<T1> GetRoot<T1>(T1 depArg)
    where T1: notnull
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

    return new Program<T1>(new Service<T1, int, List<T1>, Dictionary<T1, int>>(new Dependency<T1>(depArg), _root._singletonDependencyStruct51));
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+ProgramᐸT1ᐳ GetRootᐸT1ᐳ(T1 depArg)
	}
	ServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ --|> IServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ
	class ServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ {
		+Service(IDependencyᐸT1ᐳ dependency1, IDependencyᐸInt32ᐳ dependency2)
	}
	DependencyStructᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : "value type" 
	class DependencyStructᐸInt32ᐳ {
		+DependencyStruct()
	}
	DependencyᐸT1ᐳ --|> IDependencyᐸT1ᐳ
	class DependencyᐸT1ᐳ {
		+Dependency(T1 value)
	}
	class IServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ {
		<<interface>>
	}
	class IDependencyᐸInt32ᐳ {
		<<interface>>
	}
	class IDependencyᐸT1ᐳ {
		<<interface>>
	}
	Composition ..> ProgramᐸT1ᐳ : ProgramᐸT1ᐳ GetRootᐸT1ᐳ(T1 depArg)
	ProgramᐸT1ᐳ *--  ServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ : IServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ
	ServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ *--  DependencyᐸT1ᐳ : IDependencyᐸT1ᐳ
	ServiceᐸT1ˏInt32ˏListᐸT1ᐳˏDictionaryᐸT1ˏInt32ᐳᐳ o-- "Singleton" DependencyStructᐸInt32ᐳ : "value type"  IDependencyᐸInt32ᐳ
	DependencyᐸT1ᐳ o-- T1 : Argument "depArg"
```

