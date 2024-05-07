## Enum details

Creating an object graph of 12 transient objects, including 1 transient enumerable object.

### Class diagram
```mermaid
classDiagram
	class Enum {
		+CompositionRoot TestPureDIByCR()
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	class CompositionRoot {
		+CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)
	}
	Service1 --|> IService1 : 
	class Service1 {
		+Service1(IService2 service2)
	}
	Service2Enum --|> IService2 : 
	class Service2Enum {
		+Service2Enum(IEnumerableᐸIService3ᐳ services)
	}
	Service3 --|> IService3 : 
	class Service3 {
		+Service3(IService4 service41, IService4 service42)
	}
	Service3v2 --|> IService3 : 2 
	class Service3v2 {
		+Service3v2(IService4 service41, IService4 service42)
	}
	Service3v3 --|> IService3 : 3 
	class Service3v3 {
		+Service3v3(IService4 service41, IService4 service42)
	}
	Service3v4 --|> IService3 : 4 
	class Service3v4 {
		+Service3v4(IService4 service41, IService4 service42)
	}
	Service4 --|> IService4 : 
	class Service4 {
		+Service4()
	}
	class IEnumerableᐸIService3ᐳ
	class IService1 {
		<<abstract>>
	}
	class IService2 {
		<<abstract>>
	}
	class IService3 {
		<<abstract>>
	}
	class IService4 {
		<<abstract>>
	}
	CompositionRoot *--  Service1 : IService1
	CompositionRoot *--  Service2Enum : IService2
	CompositionRoot *--  Service2Enum : IService2
	CompositionRoot *--  Service2Enum : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot *--  Service4 : IService4
	CompositionRoot *--  Service4 : IService4
	Service1 *--  Service2Enum : IService2
	Service2Enum o--  "PerBlock" IEnumerableᐸIService3ᐳ : IEnumerableᐸIService3ᐳ
	Service3 *--  Service4 : IService4
	Service3 *--  Service4 : IService4
	Service3v2 *--  Service4 : IService4
	Service3v2 *--  Service4 : IService4
	Service3v3 *--  Service4 : IService4
	Service3v3 *--  Service4 : IService4
	Service3v4 *--  Service4 : IService4
	Service3v4 *--  Service4 : IService4
	Enum ..> CompositionRoot : CompositionRoot TestPureDIByCR()
	IEnumerableᐸIService3ᐳ *--  Service3 : IService3
	IEnumerableᐸIService3ᐳ *--  Service3v2 : 2  IService3
	IEnumerableᐸIService3ᐳ *--  Service3v3 : 3  IService3
	IEnumerableᐸIService3ᐳ *--  Service3v4 : 4  IService3
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Enum</summary><blockquote>

```c#
partial class Enum
{
  private readonly Enum _root;

  public Enum()
  {
    _root = this;
  }

  internal Enum(Enum parentScope)
  {
    _root = parentScope._root;
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public partial Benchmarks.Model.CompositionRoot TestPureDIByCR()
  {
    [MethodImpl((MethodImplOptions)0x100)]
    IEnumerable<Benchmarks.Model.IService3> EnumerationOf_perBlock10_IEnumerable()
    {
        yield return new Benchmarks.Model.Service3(new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4());
        yield return new Benchmarks.Model.Service3v2(new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4());
        yield return new Benchmarks.Model.Service3v3(new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4());
        yield return new Benchmarks.Model.Service3v4(new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4());
    }
    IEnumerable<Benchmarks.Model.IService3> perBlock10_IEnumerable = EnumerationOf_perBlock10_IEnumerable();
    return new Benchmarks.Model.CompositionRoot(new Benchmarks.Model.Service1(new Benchmarks.Model.Service2Enum(perBlock10_IEnumerable)), new Benchmarks.Model.Service2Enum(perBlock10_IEnumerable), new Benchmarks.Model.Service2Enum(perBlock10_IEnumerable), new Benchmarks.Model.Service2Enum(perBlock10_IEnumerable), new Benchmarks.Model.Service3(new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4()), new Benchmarks.Model.Service4(), new Benchmarks.Model.Service4());
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return Resolver<T>.Value.Resolve(this);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return Resolver<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
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

    throw new InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
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

    throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }

  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Enum {\n" +
          "    +CompositionRoot TestPureDIByCR()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service2Enum --|> IService2 : \n" +
        "  class Service2Enum {\n" +
          "    +Service2Enum(IEnumerableᐸIService3ᐳ services)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service3v2 --|> IService3 : 2 \n" +
        "  class Service3v2 {\n" +
          "    +Service3v2(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service3v3 --|> IService3 : 3 \n" +
        "  class Service3v3 {\n" +
          "    +Service3v3(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service3v4 --|> IService3 : 4 \n" +
        "  class Service3v4 {\n" +
          "    +Service3v4(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service4 --|> IService4 : \n" +
        "  class Service4 {\n" +
          "    +Service4()\n" +
        "  }\n" +
        "  class IEnumerableᐸIService3ᐳ\n" +
        "  class IService1 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService2 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService3 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService4 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  CompositionRoot *--  Service1 : IService1\n" +
        "  CompositionRoot *--  Service2Enum : IService2\n" +
        "  CompositionRoot *--  Service2Enum : IService2\n" +
        "  CompositionRoot *--  Service2Enum : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  CompositionRoot *--  Service4 : IService4\n" +
        "  CompositionRoot *--  Service4 : IService4\n" +
        "  Service1 *--  Service2Enum : IService2\n" +
        "  Service2Enum o--  \"PerBlock\" IEnumerableᐸIService3ᐳ : IEnumerableᐸIService3ᐳ\n" +
        "  Service3 *--  Service4 : IService4\n" +
        "  Service3 *--  Service4 : IService4\n" +
        "  Service3v2 *--  Service4 : IService4\n" +
        "  Service3v2 *--  Service4 : IService4\n" +
        "  Service3v3 *--  Service4 : IService4\n" +
        "  Service3v3 *--  Service4 : IService4\n" +
        "  Service3v4 *--  Service4 : IService4\n" +
        "  Service3v4 *--  Service4 : IService4\n" +
        "  Enum ..> CompositionRoot : CompositionRoot TestPureDIByCR()\n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3 : IService3\n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v2 : 2  IService3\n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v3 : 3  IService3\n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v4 : 4  IService3";
  }

  private readonly static int _bucketSize;
  private readonly static Pair<Type, IResolver<Enum, object>>[] _buckets;

  static Enum()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<Benchmarks.Model.CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Enum, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Enum, object>>[1]
      {
         new Pair<Type, IResolver<Enum, object>>(typeof(Benchmarks.Model.CompositionRoot), valResolver_0000)
      });
  }

  private class Resolver<T>: IResolver<Enum, T>
  {
    private const string CannotResolve = "Cannot resolve composition root ";
    private const string OfType = "of type ";
    public static IResolver<Enum, T> Value = new Resolver<T>();

    public virtual T Resolve(Enum composite)
    {
      throw new InvalidOperationException($"{CannotResolve}{OfType}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Enum composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolve}\"{tag}\" {OfType}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<Benchmarks.Model.CompositionRoot>
  {
    public override Benchmarks.Model.CompositionRoot Resolve(Enum composition)
    {
      return composition.TestPureDIByCR();
    }

    public override Benchmarks.Model.CompositionRoot ResolveByTag(Enum composition, object tag)
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

</blockquote></details>

