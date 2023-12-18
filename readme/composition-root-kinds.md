#### Composition root kinds

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/CompositionRootKindsScenario.cs)

```c#
interface IDependency { }

class Dependency : IDependency { }

interface IService { }

class Service : IService
{
    public Service(IDependency dependency) { }
}

class OtherService : IService
{
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
                // Creates a private partial root method named "GetRoot"
                .Root<IService>("GetRoot", default, RootKinds.Private | RootKinds.Partial | RootKinds.Method)
            .Bind<IService>("Other").To<OtherService>()
                // Creates a public root method named "GetOtherService"
                .Root<IService>("GetOtherService", "Other", RootKinds.Public | RootKinds.Method)
            .Bind<IDependency>().To<Dependency>()
                // Creates a internal static root named "Dependency"
                .Root<IDependency>("Dependency", default, RootKinds.Internal | RootKinds.Static);

    private partial IService GetRoot();

    public IService Root => GetRoot();
}

var composition = new Composition();
var service = composition.Root;
var otherService = composition.GetOtherService();
var dependency = Composition.Dependency;
```
<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly global::System.IDisposable[] _disposableSingletonsM12D18di;
  
  public Composition()
  {
    _disposableSingletonsM12D18di = new global::System.IDisposable[0];
  }
  
  internal Composition(Composition parent)
  {
    _disposableSingletonsM12D18di = new global::System.IDisposable[0];
  }
  
  #region Composition Roots
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  private partial Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService GetRoot()
  {
    return new Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.Service(new Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.Dependency());
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService GetOtherService()
  {
    return new Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.OtherService();
  }
  
  internal static Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency Dependency
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      return new Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.Dependency();
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM12D18di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM12D18di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM12D18di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM12D18di;
    do {
      ref var pair = ref _bucketsM12D18di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM12D18di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM12D18di;
    do {
      ref var pair = ref _bucketsM12D18di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  #endregion
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IDependency Dependency\n" +
          "    +IService GetOtherService\n" +
          "    +IService GetRoot\n" +
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
        "  Composition ..> Service : IService GetRoot\n" +
        "  Composition ..> OtherService : \"Other\" IService GetOtherService\n" +
        "  Composition ..> Dependency : IDependency Dependency";
  }
  
  private readonly static int _bucketSizeM12D18di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM12D18di;
  
  
  static Composition()
  {
    var valResolverM12D18di_0000 = new ResolverM12D18di_0000();
    ResolverM12D18di<Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService>.Value = valResolverM12D18di_0000;
    var valResolverM12D18di_0001 = new ResolverM12D18di_0001();
    ResolverM12D18di<Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency>.Value = valResolverM12D18di_0001;
    _bucketsM12D18di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM12D18di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService), valResolverM12D18di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency), valResolverM12D18di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM12D18di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM12D18di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM12D18di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService>
  {
    public Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService Resolve(Composition composition)
    {
      return composition.GetRoot();
    }
    
    public Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, "Other")) return composition.GetOtherService();;
      if (Equals(tag, null)) return composition.GetRoot();;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IService.");
    }
  }
  
  private sealed class ResolverM12D18di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency>
  {
    public Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency Resolve(Composition composition)
    {
      return Composition.Dependency;
    }
    
    public Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, null)) return Composition.Dependency;;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.CompositionRootKindsScenario.IDependency.");
    }
  }
  #endregion
}
```

</blockquote></details>

