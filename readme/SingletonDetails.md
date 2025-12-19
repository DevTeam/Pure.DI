## Singleton details

Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .

### Class diagram
```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service1 --|> IService1
	Service2 --|> IService2
	Service3 --|> IService3
	Service4 --|> IService4
	Singleton ..> CompositionRoot : CompositionRoot TestPureDIByCR()
	Service1 *--  Service2 : IService2
	Service2 *-- "5 " Service3 : IService3
	Service3 o-- "2 Scoped instances" Service4 : IService4
	CompositionRoot o-- "Scoped" Service1 : IService1
	CompositionRoot *-- "3 " Service2 : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot o-- "2 Scoped instances" Service4 : IService4
	namespace Pure.DI.Benchmarks.Benchmarks {
		class Singleton {
		<<partial>>
		+CompositionRoot TestPureDIByCR()
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
	}
	namespace Pure.DI.Benchmarks.Model {
		class CompositionRoot {
				<<class>>
			+CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)
		}
		class IService1 {
			<<interface>>
		}
		class IService2 {
			<<interface>>
		}
		class IService3 {
			<<interface>>
		}
		class IService4 {
			<<interface>>
		}
		class Service1 {
				<<class>>
			+Service1(IService2 service2)
		}
		class Service2 {
				<<class>>
			+Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
		}
		class Service3 {
				<<class>>
			+Service3(IService4 service41, IService4 service42)
		}
		class Service4 {
				<<class>>
			+Service4()
		}
	}
```

### Generated code

The following partial class will be generated:

```c#
partial class Singleton
{
  private readonly Singleton _root;

  private Service1? _scopedService149;
  private Service4? _scopedService452;

  [OrdinalAttribute(256)]
  public Singleton()
  {
    _root = this;
  }

  internal Singleton(Singleton parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public partial CompositionRoot TestPureDIByCR()
  {
    if (_scopedService149 is null)
    {
      EnsureService4Exists();
      _scopedService149 = new Service1(new Service2(new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452)));
    }

    return new CompositionRoot(_scopedService149, new Service2(new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452)), new Service2(new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452)), new Service2(new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452), new Service3(_scopedService452, _scopedService452)), new Service3(_scopedService452, _scopedService452), _scopedService452, _scopedService452);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureService4Exists()
    {
      if (_scopedService452 is null)
      {
        _scopedService452 = new Service4();
      }
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
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new CannotResolveException($"{CannotResolveMessage} {OfTypeMessage} {type}.", type, null);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, object? tag, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new CannotResolveException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.", type, tag);
  }

  private readonly static uint _bucketSize;
  private readonly static Pair<IResolver<Singleton, object>>[] _buckets;

  static Singleton()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<IResolver<Singleton, object>>.Create(
      1,
      out _bucketSize,
      new Pair<IResolver<Singleton, object>>[1]
      {
         new Pair<IResolver<Singleton, object>>(typeof(CompositionRoot), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Singleton, T>
  {
    public static IResolver<Singleton, T> Value = new Resolver<T>();

    public virtual T Resolve(Singleton composite)
    {
      throw new CannotResolveException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.", typeof(T), null);
    }

    public virtual T ResolveByTag(Singleton composite, object tag)
    {
      throw new CannotResolveException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.", typeof(T), tag);
    }
  }

  private sealed class Resolver_0000: Resolver<CompositionRoot>
  {
    public override CompositionRoot Resolve(Singleton composition)
    {
      return composition.TestPureDIByCR();
    }

    public override CompositionRoot ResolveByTag(Singleton composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.TestPureDIByCR();

        default:
          return base.ResolveByTag(composition, tag);
      }
    }
  }
}
```
