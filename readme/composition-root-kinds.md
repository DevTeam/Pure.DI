#### Composition root kinds

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/CompositionRootKindsScenario.cs)

```c#
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    public Service(IDependency dependency) { }
}

class OtherService : IService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IDependency>().To<Dependency>()

            // Creates a public root method named "GetOtherService"
            .Root<IService>("GetOtherService", "Other", RootKinds.Public | RootKinds.Method)

            // Creates a private partial root method named "GetRoot"
            .Root<IService>("GetRoot", kind: RootKinds.Private | RootKinds.Partial | RootKinds.Method)

            // Creates a internal static root named "Dependency"
            .Root<IDependency>("Dependency", kind: RootKinds.Internal | RootKinds.Static);

    private partial IService GetRoot();

    public IService Root => GetRoot();
}

var composition = new Composition();
var service = composition.Root;
var otherService = composition.GetOtherService();
var dependency = Composition.Dependency;
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IDependency Dependency
    +IService GetOtherService()
    +IService GetRoot()
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  OtherService --|> IService : "Other" 
  class OtherService {
    +OtherService()
  }
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  class IService {
    <<abstract>>
  }
  class IDependency {
    <<abstract>>
  }
  Service *--  Dependency : IDependency
  Composition ..> OtherService : IService GetOtherService()
  Composition ..> Service : IService GetRoot()
  Composition ..> Dependency : IDependency Dependency
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM04D26di;
  
  public Composition()
  {
    _rootM04D26di = this;
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM04D26di = baseComposition._rootM04D26di;
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  private partial Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService GetRoot()
  {
    return new Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.Service(new Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.Dependency());
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService GetOtherService()
  {
    return new Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.OtherService();
  }
  
  internal static Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency Dependency
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
    get
    {
      return new Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.Dependency();
    }
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return ResolverM04D26di<T>.Value.Resolve(this);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM04D26di<T>.Value.ResolveByTag(this, tag);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D26di[index];
    return pair.Key == type ? pair.Value.Resolve(this) : ResolveM04D26di(type, index);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x8)]
  private object ResolveM04D26di(global::System.Type type, int index)
  {
    var finish = index + _bucketSizeM04D26di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D26di[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D26di[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : ResolveM04D26di(type, tag, index);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x8)]
  private object ResolveM04D26di(global::System.Type type, object? tag, int index)
  {
    var finish = index + _bucketSizeM04D26di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D26di[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IDependency Dependency\n" +
          "    +IService GetOtherService()\n" +
          "    +IService GetRoot()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  OtherService --|> IService : \"Other\" \n" +
        "  class OtherService {\n" +
          "    +OtherService()\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service *--  Dependency : IDependency\n" +
        "  Composition ..> OtherService : IService GetOtherService()\n" +
        "  Composition ..> Service : IService GetRoot()\n" +
        "  Composition ..> Dependency : IDependency Dependency";
  }
  
  private readonly static int _bucketSizeM04D26di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM04D26di;
  
  static Composition()
  {
    var valResolverM04D26di_0000 = new ResolverM04D26di_0000();
    ResolverM04D26di<Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService>.Value = valResolverM04D26di_0000;
    var valResolverM04D26di_0001 = new ResolverM04D26di_0001();
    ResolverM04D26di<Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency>.Value = valResolverM04D26di_0001;
    _bucketsM04D26di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM04D26di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService), valResolverM04D26di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency), valResolverM04D26di_0001)
      });
  }
  
  private sealed class ResolverM04D26di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM04D26di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM04D26di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService>
  {
    public Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService Resolve(Composition composition)
    {
      return composition.GetRoot();
    }
    
    public Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case "Other":
          return composition.GetOtherService();
        case null:
          return composition.GetRoot();
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IService.");
      }
    }
  }
  
  private sealed class ResolverM04D26di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency>
  {
    public Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency Resolve(Composition composition)
    {
      return Composition.Dependency;
    }
    
    public Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return Composition.Dependency;
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario.IDependency.");
      }
    }
  }
}
```

</blockquote></details>

