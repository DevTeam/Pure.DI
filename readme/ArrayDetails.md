## Array details

Creating an object graph of 27 transient objects, including 4 transient array objects.

### Class diagram
```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service1 --|> IService1
	Service2Array --|> IService2
	Service3 --|> IService3
	Service4 --|> IService4
	Service3v2 --|> IService3 : 2 
	Service3v3 --|> IService3 : 3 
	Service3v4 --|> IService3 : 4 
	Array ..> CompositionRoot : CompositionRoot TestPureDIByCR()
	CompositionRoot *--  Service1 : IService1
	CompositionRoot *-- "3 " Service2Array : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot *-- "2 " Service4 : IService4
	Service1 *--  Service2Array : IService2
	Service2Array *--  ArrayᐸIService3ᐳ : ArrayᐸIService3ᐳ
	Service3 *-- "2 " Service4 : IService4
	ArrayᐸIService3ᐳ *--  Service3 : IService3
	ArrayᐸIService3ᐳ *--  Service3v2 : 2  IService3
	ArrayᐸIService3ᐳ *--  Service3v3 : 3  IService3
	ArrayᐸIService3ᐳ *--  Service3v4 : 4  IService3
	Service3v2 *-- "2 " Service4 : IService4
	Service3v3 *-- "2 " Service4 : IService4
	Service3v4 *-- "2 " Service4 : IService4
	class ArrayᐸIService3ᐳ {
			<<array>>
	}
	namespace Pure.DI.Benchmarks.Benchmarks {
		class Array {
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
			+Service1(IService2 service2)
		}
		class Service2Array {
			+Service2Array(ArrayᐸIService3ᐳ services)
		}
		class Service3 {
			+Service3(IService4 service41, IService4 service42)
		}
		class Service3v2 {
			+Service3v2(IService4 service41, IService4 service42)
		}
		class Service3v3 {
			+Service3v3(IService4 service41, IService4 service42)
		}
		class Service3v4 {
			+Service3v4(IService4 service41, IService4 service42)
		}
		class Service4 {
			+Service4()
		}
	}
```

### Generated code

The following partial class will be generated:

```c#
partial class Array
{
  private readonly Array _root;

  [OrdinalAttribute(256)]
  public Array()
  {
    _root = this;
  }

  internal Array(Array parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public partial CompositionRoot TestPureDIByCR()
  {
    return new CompositionRoot(new Service1(new Service2Array(new IService3[4] { new Service3(new Service4(), new Service4()), new Service3v2(new Service4(), new Service4()), new Service3v3(new Service4(), new Service4()), new Service3v4(new Service4(), new Service4()) })), new Service2Array(new IService3[4] { new Service3(new Service4(), new Service4()), new Service3v2(new Service4(), new Service4()), new Service3v3(new Service4(), new Service4()), new Service3v4(new Service4(), new Service4()) }), new Service2Array(new IService3[4] { new Service3(new Service4(), new Service4()), new Service3v2(new Service4(), new Service4()), new Service3v3(new Service4(), new Service4()), new Service3v4(new Service4(), new Service4()) }), new Service2Array(new IService3[4] { new Service3(new Service4(), new Service4()), new Service3v2(new Service4(), new Service4()), new Service3v3(new Service4(), new Service4()), new Service3v4(new Service4(), new Service4()) }), new Service3(new Service4(), new Service4()), new Service4(), new Service4());
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
  private readonly static Pair<Type, IResolver<Array, object>>[] _buckets;

  static Array()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Array, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Array, object>>[1]
      {
         new Pair<Type, IResolver<Array, object>>(typeof(CompositionRoot), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Array, T>
  {
    public static IResolver<Array, T> Value = new Resolver<T>();

    public virtual T Resolve(Array composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Array composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<CompositionRoot>
  {
    public override CompositionRoot Resolve(Array composition)
    {
      return composition.TestPureDIByCR();
    }

    public override CompositionRoot ResolveByTag(Array composition, object tag)
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
