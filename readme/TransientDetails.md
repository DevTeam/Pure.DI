## Transient details

Creating an object graph of 22 transient objects.

### Class diagram
```mermaid
classDiagram
	class Transient {
		<<partial>>
		+CompositionRoot TestPureDIByCR()
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	class CompositionRoot {
		+CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)
	}
	Service1 --|> IService1
	class Service1 {
		+Service1(IService2 service2)
	}
	Service2 --|> IService2
	class Service2 {
		+Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
	}
	Service3 --|> IService3
	class Service3 {
		+Service3(IService4 service41, IService4 service42)
	}
	Service4 --|> IService4
	class Service4 {
		+Service4()
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
	CompositionRoot *--  Service1 : IService1
	CompositionRoot *-- "3 " Service2 : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot *-- "2 " Service4 : IService4
	Service1 *--  Service2 : IService2
	Service2 *-- "5 " Service3 : IService3
	Service3 *-- "2 " Service4 : IService4
	Transient ..> CompositionRoot : CompositionRoot TestPureDIByCR()
```

### Generated code

The following partial class will be generated:

```c#
partial class Transient
{
  private readonly Transient _root;

  public Transient()
  {
    _root = this;
  }

  internal Transient(Transient parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public partialCompositionRoot TestPureDIByCR()
  {
    return newCompositionRoot(newService1(newService2(newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()))), newService2(newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4())), newService2(newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4())), newService2(newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4()), newService3(newService4(), newService4())), newService3(newService4(), newService4()), newService4(), newService4());
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
  private readonly static Pair<Type, IResolver<Transient, object>>[] _buckets;

  static Transient()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<Benchmarks.Model.CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Transient, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Transient, object>>[1]
      {
         new Pair<Type, IResolver<Transient, object>>(typeof(Benchmarks.Model.CompositionRoot), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Transient, T>
  {
    public static IResolver<Transient, T> Value = new Resolver<T>();

    public virtual T Resolve(Transient composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Transient composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<Benchmarks.Model.CompositionRoot>
  {
    public overrideCompositionRoot Resolve(Transient composition)
    {
      return composition.TestPureDIByCR();
    }

    public overrideCompositionRoot ResolveByTag(Transient composition, object tag)
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
