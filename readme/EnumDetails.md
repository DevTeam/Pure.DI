## Enum details

Creating an object graph of 12 transient objects, including 1 transient enumerable object.

### Class diagram
```mermaid
classDiagram
  class Enum {
    +ICompositionRoot Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class IEnumerableᐸIService3ᐳ
  CompositionRoot --|> ICompositionRoot : 
  class CompositionRoot {
    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)
  }
  Service1 --|> IService1 : 
  class Service1 {
    +Service1(IService2 service2)
  }
  Service2Enum --|> IService2 : 
  class Service2Enum {
    +Service2Enum(IEnumerableᐸIService3ᐳ services)
  }
  Service3 --|> IService3 : 
  class Service3 {
    +Service3()
  }
  Service3v2 --|> IService3 : 2 
  class Service3v2 {
    +Service3v2()
  }
  Service3v3 --|> IService3 : 3 
  class Service3v3 {
    +Service3v3()
  }
  Service3v4 --|> IService3 : 4 
  class Service3v4 {
    +Service3v4()
  }
  class ICompositionRoot {
    <<abstract>>
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
  IEnumerableᐸIService3ᐳ *--  Service3 : 
  IEnumerableᐸIService3ᐳ *--  Service3v2 : 2  
  IEnumerableᐸIService3ᐳ *--  Service3v3 : 3  
  IEnumerableᐸIService3ᐳ *--  Service3v4 : 4  
  CompositionRoot *--  Service1 : IService1 service1
  CompositionRoot *--  Service2Enum : IService2 service21
  CompositionRoot *--  Service2Enum : IService2 service22
  CompositionRoot *--  Service2Enum : IService2 service23
  CompositionRoot *--  Service3 : IService3 service3
  Service1 *--  Service2Enum : IService2 service2
  Service2Enum o--  "PerResolve" IEnumerableᐸIService3ᐳ : IEnumerableᐸIService3ᐳ services
  Enum ..> CompositionRoot : ICompositionRoot Root
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Enum</summary><blockquote>

```c#
partial class Enum
{
  public Enum()
  {
  }
  
  internal Enum(Enum parent)
  {
  }
  
  #region Composition Roots
  public Pure.DI.Benchmarks.Model.ICompositionRoot Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      System.Collections.Generic.IEnumerable<Pure.DI.Benchmarks.Model.IService3> LocalFunc_perResolveM08D02di_0006()
      {
          Pure.DI.Benchmarks.Model.Service3 transientM08D02di_0020 = new Pure.DI.Benchmarks.Model.Service3();
          yield return transientM08D02di_0020;
          Pure.DI.Benchmarks.Model.Service3v2 transientM08D02di_0021 = new Pure.DI.Benchmarks.Model.Service3v2();
          yield return transientM08D02di_0021;
          Pure.DI.Benchmarks.Model.Service3v3 transientM08D02di_0022 = new Pure.DI.Benchmarks.Model.Service3v3();
          yield return transientM08D02di_0022;
          Pure.DI.Benchmarks.Model.Service3v4 transientM08D02di_0023 = new Pure.DI.Benchmarks.Model.Service3v4();
          yield return transientM08D02di_0023;
      }
      System.Collections.Generic.IEnumerable<Pure.DI.Benchmarks.Model.IService3> perResolveM08D02di_0006 = LocalFunc_perResolveM08D02di_0006();
      Pure.DI.Benchmarks.Model.Service2Enum transientM08D02di_0019 = new Pure.DI.Benchmarks.Model.Service2Enum(perResolveM08D02di_0006);
      Pure.DI.Benchmarks.Model.Service1 transientM08D02di_0001 = new Pure.DI.Benchmarks.Model.Service1(transientM08D02di_0019);
      Pure.DI.Benchmarks.Model.Service2Enum transientM08D02di_0002 = new Pure.DI.Benchmarks.Model.Service2Enum(perResolveM08D02di_0006);
      Pure.DI.Benchmarks.Model.Service2Enum transientM08D02di_0003 = new Pure.DI.Benchmarks.Model.Service2Enum(perResolveM08D02di_0006);
      Pure.DI.Benchmarks.Model.Service2Enum transientM08D02di_0004 = new Pure.DI.Benchmarks.Model.Service2Enum(perResolveM08D02di_0006);
      Pure.DI.Benchmarks.Model.Service3 transientM08D02di_0005 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.CompositionRoot transientM08D02di_0000 = new Pure.DI.Benchmarks.Model.CompositionRoot(transientM08D02di_0001, transientM08D02di_0004, transientM08D02di_0003, transientM08D02di_0002, transientM08D02di_0005);
      return transientM08D02di_0000;
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
    return ResolverM08D02di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM08D02di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM08D02di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM08D02di;
    do {
      ref var pair = ref _bucketsM08D02di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM08D02di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM08D02di;
    do {
      ref var pair = ref _bucketsM08D02di[index];
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
        "  class Enum {\n" +
          "    +ICompositionRoot Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class IEnumerableᐸIService3ᐳ\n" +
        "  CompositionRoot --|> ICompositionRoot : \n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service2Enum --|> IService2 : \n" +
        "  class Service2Enum {\n" +
          "    +Service2Enum(IEnumerableᐸIService3ᐳ services)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3()\n" +
        "  }\n" +
        "  Service3v2 --|> IService3 : 2 \n" +
        "  class Service3v2 {\n" +
          "    +Service3v2()\n" +
        "  }\n" +
        "  Service3v3 --|> IService3 : 3 \n" +
        "  class Service3v3 {\n" +
          "    +Service3v3()\n" +
        "  }\n" +
        "  Service3v4 --|> IService3 : 4 \n" +
        "  class Service3v4 {\n" +
          "    +Service3v4()\n" +
        "  }\n" +
        "  class ICompositionRoot {\n" +
          "    <<abstract>>\n" +
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
        "  IEnumerableᐸIService3ᐳ *--  Service3 : \n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v2 : 2  \n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v3 : 3  \n" +
        "  IEnumerableᐸIService3ᐳ *--  Service3v4 : 4  \n" +
        "  CompositionRoot *--  Service1 : IService1 service1\n" +
        "  CompositionRoot *--  Service2Enum : IService2 service21\n" +
        "  CompositionRoot *--  Service2Enum : IService2 service22\n" +
        "  CompositionRoot *--  Service2Enum : IService2 service23\n" +
        "  CompositionRoot *--  Service3 : IService3 service3\n" +
        "  Service1 *--  Service2Enum : IService2 service2\n" +
        "  Service2Enum o--  \"PerResolve\" IEnumerableᐸIService3ᐳ : IEnumerableᐸIService3ᐳ services\n" +
        "  Enum ..> CompositionRoot : ICompositionRoot Root";
  }
  
  private readonly static int _bucketSizeM08D02di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Enum, object>>[] _bucketsM08D02di;
  
  static Enum()
  {
    ResolverM08D02di_0000 valResolverM08D02di_0000 = new ResolverM08D02di_0000();
    ResolverM08D02di<Pure.DI.Benchmarks.Model.ICompositionRoot>.Value = valResolverM08D02di_0000;
    _bucketsM08D02di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Enum, object>>.Create(
      1,
      out _bucketSizeM08D02di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Enum, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Enum, object>>(typeof(Pure.DI.Benchmarks.Model.ICompositionRoot), valResolverM08D02di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM08D02di<T>: global::Pure.DI.IResolver<Enum, T>
  {
    public static global::Pure.DI.IResolver<Enum, T> Value = new ResolverM08D02di<T>();
    
    public T Resolve(Enum composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Enum composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM08D02di_0000: global::Pure.DI.IResolver<Enum, Pure.DI.Benchmarks.Model.ICompositionRoot>
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot Resolve(Enum composition)
    {
      return composition.Root;
    }
    
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot ResolveByTag(Enum composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.Benchmarks.Model.ICompositionRoot.");
    }
  }
  #endregion
}
```

</blockquote></details>

