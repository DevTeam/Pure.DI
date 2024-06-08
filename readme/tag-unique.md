#### Tag Unique

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagUniqueScenario.cs)

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.


```c#
interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = [..dependencies];
}

DI.Setup(nameof(Composition))
    .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var stringService = composition.Root;
stringService.Dependencies.Length.ShouldBe(2);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  public IService<string> Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      IEnumerable<IDependency<string>> EnumerationOf_perBlockIEnumerable1()
      {
          yield return new AbcDependency<string>();
          yield return new XyzDependency<string>();
      }

      IEnumerable<IDependency<string>> perBlockIEnumerable1 = EnumerationOf_perBlockIEnumerable1();
      return new Service<string>(perBlockIEnumerable1);
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
    Resolver<IService<string>>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Composition, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Composition, object>>[1]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof(IService<string>), valResolver_0000)
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

  private sealed class Resolver_0000: Resolver<IService<string>>
  {
    public override IService<string> Resolve(Composition composition)
    {
      return composition.Root;
    }

    public override IService<string> ResolveByTag(Composition composition, object tag)
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
		+IServiceᐸStringᐳ Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	ServiceᐸStringᐳ --|> IServiceᐸStringᐳ
	class ServiceᐸStringᐳ {
		+Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)
	}
	class IEnumerableᐸIDependencyᐸStringᐳᐳ
	AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.AbcDependency<Pure.DI.TT>) 
	class AbcDependencyᐸStringᐳ {
		+AbcDependency()
	}
	XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.XyzDependency<Pure.DI.TT>) 
	class XyzDependencyᐸStringᐳ {
		+XyzDependency()
	}
	class IServiceᐸStringᐳ {
		<<interface>>
	}
	class IDependencyᐸStringᐳ {
		<<interface>>
	}
	Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ Root
	ServiceᐸStringᐳ o-- "PerBlock" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.AbcDependency<Pure.DI.TT>)  IDependencyᐸStringᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : typeof(Pure.DI.UsageTests.Advanced.TagUniqueScenario.XyzDependency<Pure.DI.TT>)  IDependencyᐸStringᐳ
```

