#### Tracking disposable instances per a composition root

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Hints/TrackingDisposableInstancesPerRootScenario.cs)

```c#
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

internal static class Disposables
{
    public static readonly IDisposable Empty = new EmptyDisposable();

    public static IDisposable Combine(this Stack<IDisposable> disposables) =>
        new CombinedDisposable(disposables);

    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    internal class CombinedDisposable(Stack<IDisposable> disposables)
        : IDisposable
    {
        public void Dispose()
        {
            while (disposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        }
    }
}

partial class Composition
{
    private readonly ConcurrentDictionary<int, Stack<IDisposable>> _disposables = new();

    private void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to call a partial method
            // named OnNewInstance when an instance is created
            .Hint(Hint.OnNewInstance, "On")

            // Specifies to call the partial method
            // only for instances with lifetime
            // Transient, PerResolve and PerBlock
            .Hint(
                Hint.OnNewInstanceLifetimeRegularExpression,
                "Transient|PerResolve|PerBlock")

            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind().To(_ =>
                _disposables.TryRemove(Environment.CurrentManagedThreadId, out var disposables)
                    ? disposables.Combine()
                    : Disposables.Empty)

            .Root<(IService service, IDisposable combinedDisposables)>("Root");

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime)
    {
        if (value is IDisposable disposable && value is not Disposables.CombinedDisposable)
        {
            var disposables = _disposables.GetOrAdd(
                Environment.CurrentManagedThreadId,
                _ => new Stack<IDisposable>());

            disposables.Push(disposable);
        }
    }
}

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
        
root2.combinedDisposables.Dispose();
        
// Checks that the disposable instances
// associated with root1 have been disposed of
root2.service.Dependency.IsDisposed.ShouldBeTrue();
        
// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.service.Dependency.IsDisposed.ShouldBeFalse();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +ValueTupleᐸIServiceˏIDisposableᐳ Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class ValueTupleᐸIServiceˏIDisposableᐳ {
    +ValueTuple(IService item1, IDisposable item2)
  }
  class IDisposable
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  ValueTupleᐸIServiceˏIDisposableᐳ *--  Service : IService
  ValueTupleᐸIServiceˏIDisposableᐳ *--  IDisposable : IDisposable
  Service *--  Dependency : IDependency
  Composition ..> ValueTupleᐸIServiceˏIDisposableᐳ : ValueTupleᐸIServiceˏIDisposableᐳ Root
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM03D17di;
  
  public Composition()
  {
    _rootM03D17di = this;
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM03D17di = baseComposition._rootM03D17di;
  }
  
  public (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables) Root
  {
    get
    {
      Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Dependency transientM03D17di3_Dependency = new Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Dependency();
      OnNewInstance<Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Dependency>(ref transientM03D17di3_Dependency, null, Pure.DI.Lifetime.Transient);
      System.IDisposable transientM03D17di2_IDisposable = _disposables.TryRemove(Environment.CurrentManagedThreadId, out var disposables_M03D17di1) ? disposables_M03D17di1.Combine() : Disposables.Empty;
      OnNewInstance<System.IDisposable>(ref transientM03D17di2_IDisposable, null, Pure.DI.Lifetime.Transient);
      Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Service transientM03D17di1_Service = new Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Service(transientM03D17di3_Dependency);
      OnNewInstance<Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.Service>(ref transientM03D17di1_Service, null, Pure.DI.Lifetime.Transient);
      (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables) transientM03D17di0_ValueTuple = (transientM03D17di1_Service, transientM03D17di2_IDisposable);
      OnNewInstance<(Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables)>(ref transientM03D17di0_ValueTuple, null, Pure.DI.Lifetime.Transient);
      return transientM03D17di0_ValueTuple;
    }
  }
  
  public T Resolve<T>()
  {
    return ResolverM03D17di<T>.Value.Resolve(this);
  }
  
  public T Resolve<T>(object? tag)
  {
    return ResolverM03D17di<T>.Value.ResolveByTag(this, tag);
  }
  
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D17di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D17di;
    do {
      ref var pair = ref _bucketsM03D17di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D17di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D17di;
    do {
      ref var pair = ref _bucketsM03D17di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  partial void OnNewInstance<T>(ref T value, object? tag, global::Pure.DI.Lifetime lifetime);
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +ValueTupleᐸIServiceˏIDisposableᐳ Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class ValueTupleᐸIServiceˏIDisposableᐳ {\n" +
          "    +ValueTuple(IService item1, IDisposable item2)\n" +
        "  }\n" +
        "  class IDisposable\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  ValueTupleᐸIServiceˏIDisposableᐳ *--  Service : IService\n" +
        "  ValueTupleᐸIServiceˏIDisposableᐳ *--  IDisposable : IDisposable\n" +
        "  Service *--  Dependency : IDependency\n" +
        "  Composition ..> ValueTupleᐸIServiceˏIDisposableᐳ : ValueTupleᐸIServiceˏIDisposableᐳ Root";
  }
  
  private readonly static int _bucketSizeM03D17di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D17di;
  
  static Composition()
  {
    var valResolverM03D17di_0000 = new ResolverM03D17di_0000();
    ResolverM03D17di<(Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables)>.Value = valResolverM03D17di_0000;
    _bucketsM03D17di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM03D17di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof((Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables)), valResolverM03D17di_0000)
      });
  }
  
  private sealed class ResolverM03D17di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D17di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D17di_0000: global::Pure.DI.IResolver<Composition, (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables)>, global::Pure.DI.IResolver<Composition, object>
  {
    public (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables) Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables) ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type (Pure.DI.UsageTests.Hints.TrackingDisposableInstancesPerRootScenario.IService service, System.IDisposable combinedDisposables).");
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

