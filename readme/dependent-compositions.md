#### Dependent compositions

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/DependentCompositionsScenario.cs)

The _Setup_ method has an additional argument _kind_, which defines the type of composition:
- _CompositionKind.Public_ - will create a normal composition class, this is the default setting and can be omitted, it can also use the _DependsOn_ method to use it as a dependency in other compositions
- _CompositionKind.Internal_ - the composition class will not be created, but that composition can be used to create other compositions by calling the _DependsOn_ method with its name
- _CompositionKind.Global_ - the composition class will also not be created, but that composition will automatically be used to create other compositions

```c#
interface IDependency { }

class Dependency : IDependency { }

interface IService { }

class Service : IService
{
    public Service(IDependency dependency) { }
}

class Program
{
    public Program(IService service) =>
        Service = service;

    public IService Service { get; }
}

// This setup does not generate code, but can be used as a dependency
// and requires the use of the "DependsOn" call to add it as a dependency
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind<IDependency>().To<Dependency>();
        
// This setup generates code and can also be used as a dependency
DI.Setup("Composition")
    // Uses "BaseComposition" setup
    .DependsOn("BaseComposition")
    .Bind<IService>().To<Service>().Root<IService>("Root");
        
// As in the previous case, this setup generates code and can also be used as a dependency
DI.Setup("OtherComposition")
    // Uses "Composition" setup
    .DependsOn("Composition")
    .Root<Program>("Program");
        
var composition = new Composition();
var service = composition.Root;

var otherComposition = new OtherComposition();
service = otherComposition.Program.Service;
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class OtherComposition {
    +Program Program
    +IService Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class Program {
    +Program(IService service)
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
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
  Program *--  Service : IService
  Service *--  Dependency : IDependency
  OtherComposition ..> Service : IService Root
  OtherComposition ..> Program : Program Program
```

</details>

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
  public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService Root
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      return new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Service(new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Dependency());
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
    var index = (int)(_bucketSizeM12D18di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
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
    var index = (int)(_bucketSizeM12D18di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
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
          "    +IService Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
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
        "  Composition ..> Service : IService Root";
  }
  
  private readonly static int _bucketSizeM12D18di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM12D18di;
  
  
  static Composition()
  {
    var valResolverM12D18di_0000 = new ResolverM12D18di_0000();
    ResolverM12D18di<Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService>.Value = valResolverM12D18di_0000;
    _bucketsM12D18di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM12D18di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService), valResolverM12D18di_0000)
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
  
  private sealed class ResolverM12D18di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService>
  {
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService.");
    }
  }
  #endregion
}
```

</blockquote></details>

<details>
<summary>Pure.DI-generated partial class OtherComposition</summary><blockquote>

```c#
partial class OtherComposition
{
  private readonly global::System.IDisposable[] _disposableSingletonsM12D18di;
  
  public OtherComposition()
  {
    _disposableSingletonsM12D18di = new global::System.IDisposable[0];
  }
  
  internal OtherComposition(OtherComposition parent)
  {
    _disposableSingletonsM12D18di = new global::System.IDisposable[0];
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService Root
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      return new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Service(new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Dependency());
    }
  }
  
  public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program Program
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      return new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program(new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Service(new Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Dependency()));
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
        "  class OtherComposition {\n" +
          "    +Program Program\n" +
          "    +IService Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class Program {\n" +
          "    +Program(IService service)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
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
        "  Program *--  Service : IService\n" +
        "  Service *--  Dependency : IDependency\n" +
        "  OtherComposition ..> Service : IService Root\n" +
        "  OtherComposition ..> Program : Program Program";
  }
  
  private readonly static int _bucketSizeM12D18di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<OtherComposition, object>>[] _bucketsM12D18di;
  
  
  static OtherComposition()
  {
    var valResolverM12D18di_0000 = new ResolverM12D18di_0000();
    ResolverM12D18di<Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService>.Value = valResolverM12D18di_0000;
    var valResolverM12D18di_0001 = new ResolverM12D18di_0001();
    ResolverM12D18di<Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program>.Value = valResolverM12D18di_0001;
    _bucketsM12D18di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<OtherComposition, object>>.Create(
      4,
      out _bucketSizeM12D18di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<OtherComposition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<OtherComposition, object>>(typeof(Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService), valResolverM12D18di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<OtherComposition, object>>(typeof(Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program), valResolverM12D18di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM12D18di<T>: global::Pure.DI.IResolver<OtherComposition, T>
  {
    public static global::Pure.DI.IResolver<OtherComposition, T> Value = new ResolverM12D18di<T>();
    
    public T Resolve(OtherComposition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(OtherComposition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM12D18di_0000: global::Pure.DI.IResolver<OtherComposition, Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService>
  {
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService Resolve(OtherComposition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService ResolveByTag(OtherComposition composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.DependentCompositionsScenario.IService.");
    }
  }
  
  private sealed class ResolverM12D18di_0001: global::Pure.DI.IResolver<OtherComposition, Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program>
  {
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program Resolve(OtherComposition composition)
    {
      return composition.Program;
    }
    
    public Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program ResolveByTag(OtherComposition composition, object tag)
    {
      if (Equals(tag, null)) return composition.Program;;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.DependentCompositionsScenario.Program.");
    }
  }
  #endregion
}
```

</blockquote></details>

