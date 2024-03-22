#### Accumulators

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/AccumulatorScenario.cs)

Accumulators allow you to accumulate instances of certain types and lifetimes.

```c#
interface IAccumulating { }

class MyAccumulator: List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService: IAccumulating
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}

DI.Setup(nameof(Composition))
    .Accumulate<IAccumulating, MyAccumulator>(Lifetime.Transient, Lifetime.Singleton)
    .Bind<IDependency>().As(Lifetime.PerBlock).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()
    .Root<(IService service, MyAccumulator accumulator)>("Root");

var composition = new Composition();
var root = composition.Root;
var service = root.service;
var accumulator = root.accumulator;
accumulator.Count.ShouldBe(3);
accumulator[0].ShouldBeOfType<AbcDependency>();
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
  private readonly Composition _rootM03D22di;
  private readonly object _lockM03D22di;
  private Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency _singletonM03D22di38_XyzDependency;
  
  public Composition()
  {
    _rootM03D22di = this;
    _lockM03D22di = new object();
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM03D22di = baseComposition._rootM03D22di;
    _lockM03D22di = _rootM03D22di._lockM03D22di;
  }
  
  public (Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator) Root
  {
    get
    {
      var accumulatorM03D22di41 = new Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator();
      Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency perBlockM03D22di4_AbcDependency = new Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency();
      accumulatorM03D22di41.Add(perBlockM03D22di4_AbcDependency);
      if (ReferenceEquals(_rootM03D22di._singletonM03D22di38_XyzDependency, null))
      {
          lock (_lockM03D22di)
          {
              if (ReferenceEquals(_rootM03D22di._singletonM03D22di38_XyzDependency, null))
              {
                  _singletonM03D22di38_XyzDependency = new Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency();
                  _rootM03D22di._singletonM03D22di38_XyzDependency = _singletonM03D22di38_XyzDependency;
              }
          }
      }
      Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency transientM03D22di3_AbcDependency = new Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency();
      accumulatorM03D22di41.Add(transientM03D22di3_AbcDependency);
      Pure.DI.UsageTests.Advanced.AccumulatorScenario.Service transientM03D22di1_Service = new Pure.DI.UsageTests.Advanced.AccumulatorScenario.Service(transientM03D22di3_AbcDependency, _rootM03D22di._singletonM03D22di38_XyzDependency, perBlockM03D22di4_AbcDependency);
      accumulatorM03D22di41.Add(transientM03D22di1_Service);
      return (transientM03D22di1_Service, accumulatorM03D22di41);
    }
  }
  
  public T Resolve<T>()
  {
    return ResolverM03D22di<T>.Value.Resolve(this);
  }
  
  public T Resolve<T>(object? tag)
  {
    return ResolverM03D22di<T>.Value.ResolveByTag(this, tag);
  }
  
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D22di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D22di;
    do {
      ref var pair = ref _bucketsM03D22di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D22di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D22di;
    do {
      ref var pair = ref _bucketsM03D22di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
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
        "  XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency) \n" +
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
        "  Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency)  IDependency\n" +
        "  Service o--  \"Singleton\" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency)  IDependency\n" +
        "  Service o--  \"PerBlock\" AbcDependency : IDependency\n" +
        "  Composition ..> ValueTupleᐸIServiceˏMyAccumulatorᐳ : ValueTupleᐸIServiceˏMyAccumulatorᐳ Root";
  }
  
  private readonly static int _bucketSizeM03D22di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D22di;
  
  static Composition()
  {
    var valResolverM03D22di_0000 = new ResolverM03D22di_0000();
    ResolverM03D22di<(Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator)>.Value = valResolverM03D22di_0000;
    _bucketsM03D22di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM03D22di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof((Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator)), valResolverM03D22di_0000)
      });
  }
  
  private sealed class ResolverM03D22di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D22di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D22di_0000: global::Pure.DI.IResolver<Composition, (Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator)>, global::Pure.DI.IResolver<Composition, object>
  {
    public (Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator) Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public (Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator) ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type (Pure.DI.UsageTests.Advanced.AccumulatorScenario.IService service, Pure.DI.UsageTests.Advanced.AccumulatorScenario.MyAccumulator accumulator).");
    }
    object global::Pure.DI.IResolver<Composition, object>.Resolve(Composition composition)
    {
      return Resolve(composition);
    }
    
    object global::Pure.DI.IResolver<Composition, object>.ResolveByTag(Composition composition, object tag)
    {
      return ResolveByTag(composition, tag);
    }
  }
}
```

</blockquote></details>

