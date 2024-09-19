#### Func with arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/FuncWithArgumentsScenario.cs)


```c#
interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    int Id { get; }

    int SubId { get; }
}

class Dependency(
    IClock clock,
    int id,
    [Tag("sub")] int subId)
    : IDependency
{
    public int Id => id;

    public int SubId => subId;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service : IService
{
    public Service(Func<int, int, IDependency> dependencyFactory) =>
        Dependencies =
        [
            ..Enumerable
                .Range(0, 10)
                .Select((_, index) => dependencyFactory(index, 99))
        ];

    public ImmutableArray<IDependency> Dependencies { get; }
}

DI.Setup(nameof(Composition))
    .Bind<IClock>().As(Lifetime.Singleton).To<Clock>()
    // Binds a dependency of type int
    // to the source code statement "dependencyId"
    .Bind<int>().To<int>("dependencyId")
    // Binds a dependency of type int with tag "sub"
    // to the source code statement "subId"
    .Bind<int>("sub").To<int>("subId")
    .Bind<Func<int, int, IDependency>>()
    .To<Func<int, int, IDependency>>(ctx =>
        (dependencyId, subId) =>
        {
            // Builds up an instance of type Dependency
            // referring source code statements "dependencyId"
            // and source code statements "subId"
            ctx.Inject<Dependency>(out var dependency);
            return dependency;
        })
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(10);
service.Dependencies[3].Id.ShouldBe(3);
service.Dependencies[3].SubId.ShouldBe(99);
```

Using a binding of the form `.Bind<T>().To<T>("some statement")` is a kind of hack that allows you to replace an injection with just its own string.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

  private Clock? _singletonClock39;

  [OrdinalAttribute(20)]
  public Composition()
  {
    _root = this;
    _lock = new object();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<int, int, IDependency> transientFunc1 =
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      (dependencyId, subId) =>
      {
        // Builds up an instance of type Dependency
        // referring source code statements "dependencyId"
        // and source code statements "subId"
        int transientInt324 = subId;
        int transientInt323 = dependencyId;
        if (_root._singletonClock39 == null)
        {
          lock (_lock)
          {
            if (_root._singletonClock39 == null)
            {
              _root._singletonClock39 = new Clock();
            }
          }
        }

        Dependency localDependency31 = new Dependency(_root._singletonClock39!, transientInt323, transientInt324);
        return localDependency31;
      };
      return new Service(transientFunc1);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>()
  {
    return Resolver<T>.Value.Resolve(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>(object? tag)
  {
    return Resolver<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new InvalidOperationException($"{CannotResolveMessage} {OfTypeMessage} {type}.");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, object? tag, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new InvalidOperationException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.");
  }

  private readonly static int _bucketSize;
  private readonly static Pair<Type, IResolver<Composition, object>>[] _buckets;

  static Composition()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<IService>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Composition, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Composition, object>>[1]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof(IService), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new Resolver<T>();

    public virtual T Resolve(Composition composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Composition composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<IService>
  {
    public override IService Resolve(Composition composition)
    {
      return composition.Root;
    }

    public override IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;

        default:
          return base.ResolveByTag(composition, tag);
      }
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	class Dependency {
		+Dependency(IClock clock, Int32 id, Int32 subId)
	}
	Clock --|> IClock
	class Clock {
		+Clock()
	}
	class Int32
	class FuncᐸInt32ˏInt32ˏIDependencyᐳ
	Service --|> IService
	class Service {
		+Service(FuncᐸInt32ˏInt32ˏIDependencyᐳ dependencyFactory)
	}
	class IClock {
		<<interface>>
	}
	class IService {
		<<interface>>
	}
	Dependency o-- "Singleton" Clock : IClock
	Dependency *--  Int32 : Int32
	Dependency *--  Int32 : "sub"  Int32
	Composition ..> Service : IService Root
	FuncᐸInt32ˏInt32ˏIDependencyᐳ *--  Dependency : Dependency
	Service *--  FuncᐸInt32ˏInt32ˏIDependencyᐳ : FuncᐸInt32ˏInt32ˏIDependencyᐳ
```

