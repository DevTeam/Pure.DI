#### Enumerable generics

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/EnumerableGenericsScenario.cs)

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
    .Bind<IDependency<TT>>().To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>("Xyz").To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition roots
    .Root<IService<int>>("IntRoot")
    .Root<IService<string>>("StringRoot");

var composition = new Composition();
        
var intService = composition.IntRoot;
intService.Dependencies.Length.ShouldBe(2);
intService.Dependencies[0].ShouldBeOfType<AbcDependency<int>>();
intService.Dependencies[1].ShouldBeOfType<XyzDependency<int>>();
        
var stringService = composition.StringRoot;
stringService.Dependencies.Length.ShouldBe(2);
stringService.Dependencies[0].ShouldBeOfType<AbcDependency<string>>();
stringService.Dependencies[1].ShouldBeOfType<XyzDependency<string>>();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
	class Composition {
		+IServiceᐸInt32ᐳ IntRoot
		+IServiceᐸStringᐳ StringRoot
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	ServiceᐸInt32ᐳ --|> IServiceᐸInt32ᐳ : 
	class ServiceᐸInt32ᐳ {
		+Service(IEnumerableᐸIDependencyᐸInt32ᐳᐳ dependencies)
	}
	ServiceᐸStringᐳ --|> IServiceᐸStringᐳ : 
	class ServiceᐸStringᐳ {
		+Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)
	}
	class IEnumerableᐸIDependencyᐸInt32ᐳᐳ
	class IEnumerableᐸIDependencyᐸStringᐳᐳ
	AbcDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : 
	class AbcDependencyᐸInt32ᐳ {
		+AbcDependency()
	}
	XyzDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : "Xyz" 
	class XyzDependencyᐸInt32ᐳ {
		+XyzDependency()
	}
	AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : 
	class AbcDependencyᐸStringᐳ {
		+AbcDependency()
	}
	XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : "Xyz" 
	class XyzDependencyᐸStringᐳ {
		+XyzDependency()
	}
	class IServiceᐸInt32ᐳ {
		<<abstract>>
	}
	class IServiceᐸStringᐳ {
		<<abstract>>
	}
	class IDependencyᐸInt32ᐳ {
		<<abstract>>
	}
	class IDependencyᐸStringᐳ {
		<<abstract>>
	}
	Composition ..> ServiceᐸInt32ᐳ : IServiceᐸInt32ᐳ IntRoot
	Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ StringRoot
	ServiceᐸInt32ᐳ o--  "PerBlock" IEnumerableᐸIDependencyᐸInt32ᐳᐳ : IEnumerableᐸIDependencyᐸInt32ᐳᐳ
	ServiceᐸStringᐳ o--  "PerBlock" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ
	IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  AbcDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
	IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  XyzDependencyᐸInt32ᐳ : "Xyz"  IDependencyᐸInt32ᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : IDependencyᐸStringᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : "Xyz"  IDependencyᐸStringᐳ
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM04D27di;

  public Composition()
  {
    _rootM04D27di = this;
  }

  internal Composition(Composition baseComposition)
  {
    _rootM04D27di = baseComposition._rootM04D27di;
  }

  public IService<int> IntRoot
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      [MethodImpl((MethodImplOptions)0x200)]
      System.Collections.Generic.IEnumerable<IDependency<int>> LocalperBlockM04D27di1_IEnumerable()
      {
          yield return new AbcDependency<int>();
          yield return new XyzDependency<int>();
      }
      System.Collections.Generic.IEnumerable<IDependency<int>> perBlockM04D27di1_IEnumerable = LocalperBlockM04D27di1_IEnumerable();
      return new Service<int>(perBlockM04D27di1_IEnumerable);
    }
  }

  public IService<string> StringRoot
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      [MethodImpl((MethodImplOptions)0x200)]
      System.Collections.Generic.IEnumerable<IDependency<string>> LocalperBlockM04D27di1_IEnumerable()
      {
          yield return new AbcDependency<string>();
          yield return new XyzDependency<string>();
      }
      System.Collections.Generic.IEnumerable<IDependency<string>> perBlockM04D27di1_IEnumerable = LocalperBlockM04D27di1_IEnumerable();
      return new Service<string>(perBlockM04D27di1_IEnumerable);
    }
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return ResolverM04D27di<T>.Value.Resolve(this);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM04D27di<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.Resolve(this) : ResolveM04D27di(type, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : ResolveM04D27di(type, tag, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, object? tag, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }

  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IServiceᐸInt32ᐳ IntRoot\n" +
          "    +IServiceᐸStringᐳ StringRoot\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  ServiceᐸInt32ᐳ --|> IServiceᐸInt32ᐳ : \n" +
        "  class ServiceᐸInt32ᐳ {\n" +
          "    +Service(IEnumerableᐸIDependencyᐸInt32ᐳᐳ dependencies)\n" +
        "  }\n" +
        "  ServiceᐸStringᐳ --|> IServiceᐸStringᐳ : \n" +
        "  class ServiceᐸStringᐳ {\n" +
          "    +Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)\n" +
        "  }\n" +
        "  class IEnumerableᐸIDependencyᐸInt32ᐳᐳ\n" +
        "  class IEnumerableᐸIDependencyᐸStringᐳᐳ\n" +
        "  AbcDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : \n" +
        "  class AbcDependencyᐸInt32ᐳ {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : \"Xyz\" \n" +
        "  class XyzDependencyᐸInt32ᐳ {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : \n" +
        "  class AbcDependencyᐸStringᐳ {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : \"Xyz\" \n" +
        "  class XyzDependencyᐸStringᐳ {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  class IServiceᐸInt32ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IServiceᐸStringᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸInt32ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸStringᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Composition ..> ServiceᐸInt32ᐳ : IServiceᐸInt32ᐳ IntRoot\n" +
        "  Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ StringRoot\n" +
        "  ServiceᐸInt32ᐳ o--  \"PerBlock\" IEnumerableᐸIDependencyᐸInt32ᐳᐳ : IEnumerableᐸIDependencyᐸInt32ᐳᐳ\n" +
        "  ServiceᐸStringᐳ o--  \"PerBlock\" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ\n" +
        "  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  AbcDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ\n" +
        "  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  XyzDependencyᐸInt32ᐳ : \"Xyz\"  IDependencyᐸInt32ᐳ\n" +
        "  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : IDependencyᐸStringᐳ\n" +
        "  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : \"Xyz\"  IDependencyᐸStringᐳ";
  }

  private readonly static int _bucketSizeM04D27di;
  private readonly static Pair<Type, IResolver<Composition, object>>[] _bucketsM04D27di;

  static Composition()
  {
    var valResolverM04D27di_0000 = new ResolverM04D27di_0000();
    ResolverM04D27di<IService<int>>.Value = valResolverM04D27di_0000;
    var valResolverM04D27di_0001 = new ResolverM04D27di_0001();
    ResolverM04D27di<IService<string>>.Value = valResolverM04D27di_0001;
    _bucketsM04D27di = Buckets<Type, IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM04D27di,
      new Pair<Type, IResolver<Composition, object>>[2]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof(IService<int>), valResolverM04D27di_0000)
        ,new Pair<Type, IResolver<Composition, object>>(typeof(IService<string>), valResolverM04D27di_0001)
      });
  }

  private sealed class ResolverM04D27di<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new ResolverM04D27di<T>();

    public T Resolve(Composition composite)
    {
      throw new InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }

    public T ResolveByTag(Composition composite, object tag)
    {
      throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }

  private sealed class ResolverM04D27di_0000: IResolver<Composition, IService<int>>
  {
    public IService<int> Resolve(Composition composition)
    {
      return composition.IntRoot;
    }

    public IService<int> ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.IntRoot;
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type IService<int>.");
      }
    }
  }

  private sealed class ResolverM04D27di_0001: IResolver<Composition, IService<string>>
  {
    public IService<string> Resolve(Composition composition)
    {
      return composition.StringRoot;
    }

    public IService<string> ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.StringRoot;
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type IService<string>.");
      }
    }
  }
}
```

</blockquote></details>

