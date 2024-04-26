#### Tracking async disposable instances in delegates

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/TrackingAsyncDisposableInDelegatesScenario.cs)

```c#
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(Func<Owned<IDependency>> dependencyFactory)
    : IService, IAsyncDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public ValueTask DisposeAsync()
    {
        return _dependency.DisposeAsync();
    }
}

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
}

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
        
await root2.DisposeAsync();
        
// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();
        
// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();
        
await root1.DisposeAsync();
        
// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +Service Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class Owned
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  Service --|> IAsyncDisposable : 
  class Service {
    +Service(FuncᐸOwnedᐸIDependencyᐳᐳ dependencyFactory)
  }
  class FuncᐸOwnedᐸIDependencyᐳᐳ
  class OwnedᐸIDependencyᐳ
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  class IAsyncDisposable {
    <<abstract>>
  }
  Service o--  "PerResolve" FuncᐸOwnedᐸIDependencyᐳᐳ : FuncᐸOwnedᐸIDependencyᐳᐳ
  Composition ..> Service : Service Root
  FuncᐸOwnedᐸIDependencyᐳᐳ o--  "PerBlock" OwnedᐸIDependencyᐳ : OwnedᐸIDependencyᐳ
  OwnedᐸIDependencyᐳ *--  Owned : Owned
  OwnedᐸIDependencyᐳ *--  Dependency : IDependency
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM04D26di;
  private readonly object _lockM04D26di;
  
  public Composition()
  {
    _rootM04D26di = this;
    _lockM04D26di = new object();
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM04D26di = baseComposition._rootM04D26di;
    _lockM04D26di = _rootM04D26di._lockM04D26di;
  }
  
  public Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
    get
    {
      var perResolveM04D26di39_Func = default(System.Func<Pure.DI.Owned<Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.IDependency>>);
      perResolveM04D26di39_Func = new global::System.Func<Pure.DI.Owned<Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.IDependency>>(
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
      () =>
      {
          var accumulatorM04D26di38 = new Pure.DI.Owned();
          Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Dependency transientM04D26di3_Dependency = new Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Dependency();
          lock (_lockM04D26di)
          {
              accumulatorM04D26di38.Add(transientM04D26di3_Dependency);
          }
          Pure.DI.Owned<Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.IDependency> perBlockM04D26di1_Owned;
          {
              var owned_M04D26di2 = accumulatorM04D26di38;
              var value_M04D26di3 = transientM04D26di3_Dependency;
              perBlockM04D26di1_Owned = new Owned<Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.IDependency>(value_M04D26di3, owned_M04D26di2);
          }
          lock (_lockM04D26di)
          {
              accumulatorM04D26di38.Add(perBlockM04D26di1_Owned);
          }
          var value_M04D26di1 = perBlockM04D26di1_Owned;
          return value_M04D26di1;
      });
      return new Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service(perResolveM04D26di39_Func);
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
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
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
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
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
          "    +Service Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class Owned\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  Service --|> IAsyncDisposable : \n" +
        "  class Service {\n" +
          "    +Service(FuncᐸOwnedᐸIDependencyᐳᐳ dependencyFactory)\n" +
        "  }\n" +
        "  class FuncᐸOwnedᐸIDependencyᐳᐳ\n" +
        "  class OwnedᐸIDependencyᐳ\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IAsyncDisposable {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"PerResolve\" FuncᐸOwnedᐸIDependencyᐳᐳ : FuncᐸOwnedᐸIDependencyᐳᐳ\n" +
        "  Composition ..> Service : Service Root\n" +
        "  FuncᐸOwnedᐸIDependencyᐳᐳ o--  \"PerBlock\" OwnedᐸIDependencyᐳ : OwnedᐸIDependencyᐳ\n" +
        "  OwnedᐸIDependencyᐳ *--  Owned : Owned\n" +
        "  OwnedᐸIDependencyᐳ *--  Dependency : IDependency";
  }
  
  private readonly static int _bucketSizeM04D26di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM04D26di;
  
  static Composition()
  {
    var valResolverM04D26di_0000 = new ResolverM04D26di_0000();
    ResolverM04D26di<Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service>.Value = valResolverM04D26di_0000;
    _bucketsM04D26di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM04D26di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service), valResolverM04D26di_0000)
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
  
  private sealed class ResolverM04D26di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service>
  {
    public Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.TrackingAsyncDisposableInDelegatesScenario.Service.");
      }
    }
  }
}
```

</blockquote></details>

