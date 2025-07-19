## Func details

Creating an object graph of 7 transition objects plus 1 `Func<T>` with additional 1 transition object.

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
	Service2Func --|> IService2
	Service3 --|> IService3
	Service4 --|> IService4
	Func ..> CompositionRoot : CompositionRoot TestPureDIByCR()
	Service1 *--  Service2Func : IService2
	Service2Func o-- "PerBlock" FuncᐸIService3ᐳ : FuncᐸIService3ᐳ
	Service3 *-- "2 " Service4 : IService4
	CompositionRoot *--  Service1 : IService1
	CompositionRoot *-- "3 " Service2Func : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot *-- "2 " Service4 : IService4
	FuncᐸIService3ᐳ *--  Service3 : IService3
	namespace Pure.DI.Benchmarks.Benchmarks {
		class Func {
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
		class Service2Func {
				<<class>>
			+Service2Func(FuncᐸIService3ᐳ service3Factory)
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
	namespace System {
		class FuncᐸIService3ᐳ {
				<<delegate>>
		}
	}
```

### Generated code

The following partial class will be generated:

```c#
partial class Func
{
  private readonly Func _root;

  [OrdinalAttribute(256)]
  public Func()
  {
    _root = this;
  }

  internal Func(Func parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public partial CompositionRoot TestPureDIByCR()
  {
    Func<IService3> blockFunc9 = new Func<IService3>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      IService3 localValue36 = new Service3(new Service4(), new Service4());
      return localValue36;
    });
    return new CompositionRoot(new Service1(new Service2Func(blockFunc9)), new Service2Func(blockFunc9), new Service2Func(blockFunc9), new Service2Func(blockFunc9), new Service3(new Service4(), new Service4()), new Service4(), new Service4());
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
  private readonly static Pair<Type, IResolver<Func, object>>[] _buckets;

  static Func()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Func, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Func, object>>[1]
      {
         new Pair<Type, IResolver<Func, object>>(typeof(CompositionRoot), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Func, T>
  {
    public static IResolver<Func, T> Value = new Resolver<T>();

    public virtual T Resolve(Func composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Func composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<CompositionRoot>
  {
    public override CompositionRoot Resolve(Func composition)
    {
      return composition.TestPureDIByCR();
    }

    public override CompositionRoot ResolveByTag(Func composition, object tag)
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
