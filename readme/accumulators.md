#### Accumulators

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/AccumulatorScenario.cs)

Accumulators allow you to accumulate instances of certain types and lifetimes.

```c#
interface IAccumulating;

class MyAccumulator : List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;

class XyzDependency : IDependency, IAccumulating;

interface IService;

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService, IAccumulating;

DI.Setup(nameof(Composition))
    .Accumulate<IAccumulating, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
    .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()
    .Root<(IService service, MyAccumulator accumulator)>("Root");

var composition = new Composition();
var (service, accumulator) = composition.Root;
accumulator.Count.ShouldBe(3);
accumulator[0].ShouldBeOfType<XyzDependency>();
accumulator[1].ShouldBeOfType<AbcDependency>();
accumulator[2].ShouldBeOfType<Service>();
        
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
	class Composition {
		+ValueTupleᐸIServiceˏMyAccumulatorᐳ Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	class MyAccumulator
	class ValueTupleᐸIServiceˏMyAccumulatorᐳ {
		+ValueTuple(IService item1, MyAccumulator item2)
	}
	AbcDependency --|> IDependency : 
	class AbcDependency {
		+AbcDependency()
	}
	XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency) 
	class XyzDependency {
		+XyzDependency()
	}
	Service --|> IService : 
	class Service {
		+Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)
	}
	class IDependency {
		<<abstract>>
	}
	class IService {
		<<abstract>>
	}
	ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  Service : IService
	ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  MyAccumulator : MyAccumulator
	Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency)  IDependency
	Service o--  "Singleton" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency)  IDependency
	Service o--  "PerBlock" AbcDependency : IDependency
	Composition ..> ValueTupleᐸIServiceˏMyAccumulatorᐳ : ValueTupleᐸIServiceˏMyAccumulatorᐳ Root
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM04D27di;
  private readonly object _lockM04D27di;
  private XyzDependency _singletonM04D27di38_XyzDependency;

  public Composition()
  {
    _rootM04D27di = this;
    _lockM04D27di = new object();
  }

  internal Composition(Composition baseComposition)
  {
    _rootM04D27di = baseComposition._rootM04D27di;
    _lockM04D27di = _rootM04D27di._lockM04D27di;
  }

  public (IService service, MyAccumulator accumulator) Root
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      var accumulatorM04D27di42 = new MyAccumulator();
      AbcDependency perBlockM04D27di4_AbcDependency = new AbcDependency();
      if (_rootM04D27di._singletonM04D27di38_XyzDependency == null)
      {
          lock (_lockM04D27di)
          {
              if (_rootM04D27di._singletonM04D27di38_XyzDependency == null)
              {
                  XyzDependency _singletonM04D27di38_XyzDependencyTemp;
                  _singletonM04D27di38_XyzDependencyTemp = new XyzDependency();
                  accumulatorM04D27di42.Add(_singletonM04D27di38_XyzDependencyTemp);
                  Threading.Thread.MemoryBarrier();
                  _singletonM04D27di38_XyzDependency = _singletonM04D27di38_XyzDependencyTemp;
                  _rootM04D27di._singletonM04D27di38_XyzDependency = _singletonM04D27di38_XyzDependency;
              }
          }
      }
      AbcDependency transientM04D27di3_AbcDependency = new AbcDependency();
      lock (_lockM04D27di)
      {
          accumulatorM04D27di42.Add(transientM04D27di3_AbcDependency);
      }
      Service transientM04D27di1_Service = new Service(transientM04D27di3_AbcDependency, _rootM04D27di._singletonM04D27di38_XyzDependency, perBlockM04D27di4_AbcDependency);
      lock (_lockM04D27di)
      {
          accumulatorM04D27di42.Add(transientM04D27di1_Service);
      }
      return (transientM04D27di1_Service, accumulatorM04D27di42);
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
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
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
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
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
          "    +ValueTupleᐸIServiceˏMyAccumulatorᐳ Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class MyAccumulator\n" +
        "  class ValueTupleᐸIServiceˏMyAccumulatorᐳ {\n" +
          "    +ValueTuple(IService item1, MyAccumulator item2)\n" +
        "  }\n" +
        "  AbcDependency --|> IDependency : \n" +
        "  class AbcDependency {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependency --|> IDependency : typeof(XyzDependency) \n" +
        "  class XyzDependency {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  Service : IService\n" +
        "  ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  MyAccumulator : MyAccumulator\n" +
        "  Service *--  AbcDependency : typeof(AbcDependency)  IDependency\n" +
        "  Service o--  \"Singleton\" XyzDependency : typeof(XyzDependency)  IDependency\n" +
        "  Service o--  \"PerBlock\" AbcDependency : IDependency\n" +
        "  Composition ..> ValueTupleᐸIServiceˏMyAccumulatorᐳ : ValueTupleᐸIServiceˏMyAccumulatorᐳ Root";
  }

  private readonly static int _bucketSizeM04D27di;
  private readonly static Pair<Type, IResolver<Composition, object>>[] _bucketsM04D27di;

  static Composition()
  {
    var valResolverM04D27di_0000 = new ResolverM04D27di_0000();
    ResolverM04D27di<(IService service, MyAccumulator accumulator)>.Value = valResolverM04D27di_0000;
    _bucketsM04D27di = Buckets<Type, IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM04D27di,
      new Pair<Type, IResolver<Composition, object>>[1]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof((IService service, MyAccumulator accumulator)), valResolverM04D27di_0000)
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

  private sealed class ResolverM04D27di_0000: IResolver<Composition, (IService service, MyAccumulator accumulator)>, IResolver<Composition, object>
  {
    public (IService service, MyAccumulator accumulator) Resolve(Composition composition)
    {
      return composition.Root;
    }

    public (IService service, MyAccumulator accumulator) ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type (IService service, MyAccumulator accumulator).");
      }
    }
    object IResolver<Composition, object>.Resolve(Composition composition)
    {
      return Resolve(composition);
    }

    object IResolver<Composition, object>.ResolveByTag(Composition composition, object tag)
    {
      return ResolveByTag(composition, tag);
    }
  }
}
```

</blockquote></details>

