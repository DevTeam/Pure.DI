## Singleton details

Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .

### Class diagram
```mermaid
classDiagram
	class Singleton {
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
	Service2 --|> IService2 : 
	class Service2 {
		+Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
	}
	Service3 --|> IService3 : 
	class Service3 {
		+Service3(IService4 service41, IService4 service42)
	}
	Service4 --|> IService4 : 
	class Service4 {
		+Service4()
	}
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
	CompositionRoot o--  "Scoped" Service1 : IService1
	CompositionRoot *--  Service2 : IService2
	CompositionRoot *--  Service2 : IService2
	CompositionRoot *--  Service2 : IService2
	CompositionRoot *--  Service3 : IService3
	CompositionRoot o--  "Scoped" Service4 : IService4
	CompositionRoot o--  "Scoped" Service4 : IService4
	Service1 *--  Service2 : IService2
	Service2 *--  Service3 : IService3
	Service2 *--  Service3 : IService3
	Service2 *--  Service3 : IService3
	Service2 *--  Service3 : IService3
	Service2 *--  Service3 : IService3
	Service3 o--  "Scoped" Service4 : IService4
	Service3 o--  "Scoped" Service4 : IService4
	Singleton ..> CompositionRoot : CompositionRoot TestPureDIByCR()
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Singleton</summary><blockquote>

```c#
partial class Singleton
{
  private readonly Singleton _root;
  private Benchmarks.Model.Service1? _scoped37_Service1;
  private Benchmarks.Model.Service4? _scoped40_Service4;

  public Singleton()
  {
    _root = this;
  }

  internal Singleton(Singleton parentScope)
  {
    _root = parentScope._root;
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public partial Benchmarks.Model.CompositionRoot TestPureDIByCR()
  {
    if (_scoped40_Service4 == null)
    {
        _scoped40_Service4 = new Benchmarks.Model.Service4();
    }
    if (_scoped37_Service1 == null)
    {
        _scoped37_Service1 = new Benchmarks.Model.Service1(new Benchmarks.Model.Service2(new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!)));
    }
    return new Benchmarks.Model.CompositionRoot(_scoped37_Service1!, new Benchmarks.Model.Service2(new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!)), new Benchmarks.Model.Service2(new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!)), new Benchmarks.Model.Service2(new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!)), new Benchmarks.Model.Service3(_scoped40_Service4!, _scoped40_Service4!), _scoped40_Service4!, _scoped40_Service4!);
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
        "  class Singleton {\n" +
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
        "  Service2 --|> IService2 : \n" +
        "  class Service2 {\n" +
          "    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service4 --|> IService4 : \n" +
        "  class Service4 {\n" +
          "    +Service4()\n" +
        "  }\n" +
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
        "  CompositionRoot o--  \"Scoped\" Service1 : IService1\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  CompositionRoot o--  \"Scoped\" Service4 : IService4\n" +
        "  CompositionRoot o--  \"Scoped\" Service4 : IService4\n" +
        "  Service1 *--  Service2 : IService2\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service3 o--  \"Scoped\" Service4 : IService4\n" +
        "  Service3 o--  \"Scoped\" Service4 : IService4\n" +
        "  Singleton ..> CompositionRoot : CompositionRoot TestPureDIByCR()";
  }

  private readonly static int _bucketSize;
  private readonly static Pair<Type, IResolver<Singleton, object>>[] _buckets;

  static Singleton()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<Benchmarks.Model.CompositionRoot>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Singleton, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Singleton, object>>[1]
      {
         new Pair<Type, IResolver<Singleton, object>>(typeof(Benchmarks.Model.CompositionRoot), valResolver_0000)
      });
  }

  private class Resolver<T>: IResolver<Singleton, T>
  {
    private const string CannotResolve = "Cannot resolve composition root ";
    private const string OfType = "of type ";
    public static IResolver<Singleton, T> Value = new Resolver<T>();

    public virtual T Resolve(Singleton composite)
    {
      throw new InvalidOperationException($"{CannotResolve}{OfType}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Singleton composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolve}\"{tag}\" {OfType}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<Benchmarks.Model.CompositionRoot>
  {
    public override Benchmarks.Model.CompositionRoot Resolve(Singleton composition)
    {
      return composition.TestPureDIByCR();
    }

    public override Benchmarks.Model.CompositionRoot ResolveByTag(Singleton composition, object tag)
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

