#### Enumerable generics

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/EnumerableGenericsScenario.cs)

```c#
interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = dependencies.ToImmutableArray();
}

DI.Setup("Composition")
    .Bind<IDependency<TT>>().To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>("Xyz").To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()
        .Root<IService<int>>("IntRoot")
        .Root<IService<string>>("StringRoot");

var composition = new Composition();
        
var intService = composition.IntRoot;
intService.Dependencies.Length.ShouldBe(2);
intService.Dependencies[0].ShouldBeOfType<AbcDependency<int>>();
intService.Dependencies[1].ShouldBeOfType<XyzDependency<int>>();
        
var stringService = composition.StringRoot;
stringService.Dependencies.Length.ShouldBe(2);
stringService.Dependencies[0].ShouldBeOfType<AbcDependency<string>>();
stringService.Dependencies[1].ShouldBeOfType<XyzDependency<string>>();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IServiceᐸInt32ᐳ IntRoot
    +IServiceᐸStringᐳ StringRoot
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class IEnumerableᐸIDependencyᐸStringᐳᐳ
  class IEnumerableᐸIDependencyᐸInt32ᐳᐳ
  AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : 
  class AbcDependencyᐸStringᐳ {
    +AbcDependency()
  }
  XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : "Xyz" 
  class XyzDependencyᐸStringᐳ {
    +XyzDependency()
  }
  AbcDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : 
  class AbcDependencyᐸInt32ᐳ {
    +AbcDependency()
  }
  XyzDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : "Xyz" 
  class XyzDependencyᐸInt32ᐳ {
    +XyzDependency()
  }
  ServiceᐸInt32ᐳ --|> IServiceᐸInt32ᐳ : 
  class ServiceᐸInt32ᐳ {
    +Service(IEnumerableᐸIDependencyᐸInt32ᐳᐳ dependencies)
  }
  ServiceᐸStringᐳ --|> IServiceᐸStringᐳ : 
  class ServiceᐸStringᐳ {
    +Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)
  }
  class IDependencyᐸStringᐳ {
    <<abstract>>
  }
  class IDependencyᐸInt32ᐳ {
    <<abstract>>
  }
  class IServiceᐸInt32ᐳ {
    <<abstract>>
  }
  class IServiceᐸStringᐳ {
    <<abstract>>
  }
  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : IDependencyᐸStringᐳ
  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : "Xyz"  IDependencyᐸStringᐳ
  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  AbcDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  XyzDependencyᐸInt32ᐳ : "Xyz"  IDependencyᐸInt32ᐳ
  Composition ..> ServiceᐸInt32ᐳ : IServiceᐸInt32ᐳ IntRoot
  Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ StringRoot
  ServiceᐸInt32ᐳ o--  "PerBlock" IEnumerableᐸIDependencyᐸInt32ᐳᐳ : IEnumerableᐸIDependencyᐸInt32ᐳᐳ
  ServiceᐸStringᐳ o--  "PerBlock" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly global::System.IDisposable[] _disposableSingletonsM02D01di;
  
  public Composition()
  {
    _disposableSingletonsM02D01di = new global::System.IDisposable[0];
  }
  
  internal Composition(Composition parent)
  {
    _disposableSingletonsM02D01di = new global::System.IDisposable[0];
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int> IntRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x200)]
      System.Collections.Generic.IEnumerable<Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IDependency<int>> LocalperBlockM02D01di1_IEnumerable()
      {
          yield return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.AbcDependency<int>();
          yield return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.XyzDependency<int>();
      }
      var perBlockM02D01di1_IEnumerable = LocalperBlockM02D01di1_IEnumerable();
      return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.Service<int>(perBlockM02D01di1_IEnumerable);
    }
  }
  
  public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string> StringRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x200)]
      System.Collections.Generic.IEnumerable<Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IDependency<string>> LocalperBlockM02D01di1_IEnumerable()
      {
          yield return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.AbcDependency<string>();
          yield return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.XyzDependency<string>();
      }
      var perBlockM02D01di1_IEnumerable = LocalperBlockM02D01di1_IEnumerable();
      return new Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.Service<string>(perBlockM02D01di1_IEnumerable);
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM02D01di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM02D01di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM02D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D01di;
    do {
      ref var pair = ref _bucketsM02D01di[index];
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
    var index = (int)(_bucketSizeM02D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D01di;
    do {
      ref var pair = ref _bucketsM02D01di[index];
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
          "    +IServiceᐸInt32ᐳ IntRoot\n" +
          "    +IServiceᐸStringᐳ StringRoot\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class IEnumerableᐸIDependencyᐸStringᐳᐳ\n" +
        "  class IEnumerableᐸIDependencyᐸInt32ᐳᐳ\n" +
        "  AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : \n" +
        "  class AbcDependencyᐸStringᐳ {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : \"Xyz\" \n" +
        "  class XyzDependencyᐸStringᐳ {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  AbcDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : \n" +
        "  class AbcDependencyᐸInt32ᐳ {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ : \"Xyz\" \n" +
        "  class XyzDependencyᐸInt32ᐳ {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  ServiceᐸInt32ᐳ --|> IServiceᐸInt32ᐳ : \n" +
        "  class ServiceᐸInt32ᐳ {\n" +
          "    +Service(IEnumerableᐸIDependencyᐸInt32ᐳᐳ dependencies)\n" +
        "  }\n" +
        "  ServiceᐸStringᐳ --|> IServiceᐸStringᐳ : \n" +
        "  class ServiceᐸStringᐳ {\n" +
          "    +Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)\n" +
        "  }\n" +
        "  class IDependencyᐸStringᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸInt32ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IServiceᐸInt32ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IServiceᐸStringᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : IDependencyᐸStringᐳ\n" +
        "  IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : \"Xyz\"  IDependencyᐸStringᐳ\n" +
        "  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  AbcDependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ\n" +
        "  IEnumerableᐸIDependencyᐸInt32ᐳᐳ *--  XyzDependencyᐸInt32ᐳ : \"Xyz\"  IDependencyᐸInt32ᐳ\n" +
        "  Composition ..> ServiceᐸInt32ᐳ : IServiceᐸInt32ᐳ IntRoot\n" +
        "  Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ StringRoot\n" +
        "  ServiceᐸInt32ᐳ o--  \"PerBlock\" IEnumerableᐸIDependencyᐸInt32ᐳᐳ : IEnumerableᐸIDependencyᐸInt32ᐳᐳ\n" +
        "  ServiceᐸStringᐳ o--  \"PerBlock\" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ";
  }
  
  private readonly static int _bucketSizeM02D01di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM02D01di;
  
  static Composition()
  {
    var valResolverM02D01di_0000 = new ResolverM02D01di_0000();
    ResolverM02D01di<Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int>>.Value = valResolverM02D01di_0000;
    var valResolverM02D01di_0001 = new ResolverM02D01di_0001();
    ResolverM02D01di<Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string>>.Value = valResolverM02D01di_0001;
    _bucketsM02D01di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM02D01di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int>), valResolverM02D01di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string>), valResolverM02D01di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM02D01di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM02D01di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM02D01di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int>>
  {
    public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int> Resolve(Composition composition)
    {
      return composition.IntRoot;
    }
    
    public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int> ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.IntRoot;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<int>.");
    }
  }
  
  private sealed class ResolverM02D01di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string>>
  {
    public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string> Resolve(Composition composition)
    {
      return composition.StringRoot;
    }
    
    public Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string> ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.StringRoot;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.EnumerableGenericsScenario.IService<string>.");
    }
  }
  #endregion
}
```

</blockquote></details>

