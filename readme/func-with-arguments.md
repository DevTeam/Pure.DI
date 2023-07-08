#### Func with arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/FuncWithArgumentsScenario.cs)

At any time a BCL type binding can be added manually:

```c#
internal interface IClock
{
    DateTimeOffset Now { get; }
}

internal class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

internal interface IDependency
{
    int Id { get; }
}

internal class Dependency : IDependency
{
    public Dependency(IClock clock)
    {
    }

    public int Id { get; set; }
}

internal interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

internal class Service : IService
{
    public Service(Func<int, IDependency> dependencyFactory)
    {
        Dependencies = Enumerable
            .Range(0, 10)
            .Select((_, index) => dependencyFactory(index))
            .ToImmutableArray();
    }

    public ImmutableArray<IDependency> Dependencies { get; }
}

DI.Setup("Composition")
    .Bind<IClock>().As(Lifetime.Singleton).To<Clock>()
    .Bind<Func<int, IDependency>>().To(ctx => new Func<int, IDependency>(id =>
    {
        ctx.Inject<Dependency>(out var dependency);
        dependency.Id = id;
        return dependency;
    }))
    .Bind<IService>().To<Service>().Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(10);
service.Dependencies[3].Id.ShouldBe(3);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService Root
    +T ResolveᐸTᐳ()
    +T ResolveᐸTᐳ(object? tag)
    +object ResolveᐸTᐳ(Type type)
    +object ResolveᐸTᐳ(Type type, object? tag)
  }
  class Dependency {
    +Dependency(IClock clock)
  }
  Service --|> IService : 
  class Service {
    +Service(FuncᐸInt32ˏIDependencyᐳ dependencyFactory)
  }
  Clock --|> IClock : 
  class Clock {
    +Clock()
  }
  class FuncᐸInt32ˏIDependencyᐳ
  class IService {
    <<abstract>>
  }
  class IClock {
    <<abstract>>
  }
  Dependency o--  "Singleton" Clock : IClock clock
  Service *--  FuncᐸInt32ˏIDependencyᐳ : FuncᐸInt32ˏIDependencyᐳ dependencyFactory
  Composition ..> Service : IService Root
  FuncᐸInt32ˏIDependencyᐳ *--  Dependency : Dependency
```

</details>

<details>
<summary>Composition Code</summary>

```c#
partial class Composition
{
  private readonly System.IDisposable[] _disposablesA1F7;
  private Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Clock _f22SingletonA1F7;
  
  public Composition()
  {
    _disposablesA1F7 = new System.IDisposable[0];
  }
  
  internal Composition(Composition parent)
  {
    _disposablesA1F7 = new System.IDisposable[0];
    lock (parent._disposablesA1F7)
    {
      _f22SingletonA1F7 = parent._f22SingletonA1F7;
    }
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      System.Func<int, Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IDependency> v25LocalA1F7;
      v25LocalA1F7 = new Func<int, IDependency>(id =>
      {
          if (global::System.Object.ReferenceEquals(_f22SingletonA1F7, null))
          {
              lock (_disposablesA1F7)
              {
                  if (global::System.Object.ReferenceEquals(_f22SingletonA1F7, null))
                  {
                      _f22SingletonA1F7 = new Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Clock();
                  }
              }
          }
          Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Dependency v28LocalA1F7 = new Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Dependency(_f22SingletonA1F7);
          Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Dependency dependency = v28LocalA1F7;
          dependency.Id = id;
          return dependency;
      });
      Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Service v24LocalA1F7 = new Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.Service(v25LocalA1F7);
      return v24LocalA1F7;
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>()
  {
    return ResolverA1F7<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverA1F7<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    int index = (int)(_bucketSizeA1F7 * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _bucketsA1F7[index];
    if (ReferenceEquals(pair.Key, type))
    {
      return pair.Value.Resolve(this);
    }
    
    int maxIndex = index + _bucketSizeA1F7;
    for (int i = index + 1; i < maxIndex; i++)
    {
      pair = ref _bucketsA1F7[i];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type, object? tag)
  {
    int index = (int)(_bucketSizeA1F7 * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _bucketsA1F7[index];
    if (ReferenceEquals(pair.Key, type))
    {
      return pair.Value.ResolveByTag(this, tag);
    }
    
    int maxIndex = index + _bucketSizeA1F7;
    for (int i = index + 1; i < maxIndex; i++)
    {
      pair = ref _bucketsA1F7[i];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  #endregion
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IService Root\n" +
          "    +T ResolveᐸTᐳ()\n" +
          "    +T ResolveᐸTᐳ(object? tag)\n" +
          "    +object ResolveᐸTᐳ(Type type)\n" +
          "    +object ResolveᐸTᐳ(Type type, object? tag)\n" +
        "  }\n" +
        "  class Dependency {\n" +
          "    +Dependency(IClock clock)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(FuncᐸInt32ˏIDependencyᐳ dependencyFactory)\n" +
        "  }\n" +
        "  Clock --|> IClock : \n" +
        "  class Clock {\n" +
          "    +Clock()\n" +
        "  }\n" +
        "  class FuncᐸInt32ˏIDependencyᐳ\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IClock {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Dependency o--  \"Singleton\" Clock : IClock clock\n" +
        "  Service *--  FuncᐸInt32ˏIDependencyᐳ : FuncᐸInt32ˏIDependencyᐳ dependencyFactory\n" +
        "  Composition ..> Service : IService Root\n" +
        "  FuncᐸInt32ˏIDependencyᐳ *--  Dependency : Dependency";
  }
  
  private readonly static int _bucketSizeA1F7;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsA1F7;
  
  static Composition()
  {
    ResolverA1F70 valResolverA1F70 = new ResolverA1F70();
    ResolverA1F7<Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService>.Value = valResolverA1F70;
    _bucketsA1F7 = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeA1F7,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService), valResolverA1F70)
      });
  }
  
  #region Resolvers
  private sealed class ResolverA1F7<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverA1F7<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverA1F70: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService>
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario.IService.");
    }
  }
  #endregion
}
```

</details>

