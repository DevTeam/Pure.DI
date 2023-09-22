## Singleton details

Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .

### Class diagram
```mermaid
classDiagram
  class Singleton {
    +ICompositionRoot Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  CompositionRoot --|> ICompositionRoot : 
  class CompositionRoot {
    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)
  }
  Service2 --|> IService2 : 
  class Service2 {
    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
  }
  Service1 --|> IService1 : 
  class Service1 {
    +Service1(IService2 service2)
  }
  Service3 --|> IService3 : 
  class Service3 {
    +Service3()
  }
  class ICompositionRoot {
    <<abstract>>
  }
  class IService2 {
    <<abstract>>
  }
  class IService1 {
    <<abstract>>
  }
  class IService3 {
    <<abstract>>
  }
  CompositionRoot o--  "Singleton" Service1 : IService1
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service1 *--  Service2 : IService2
  Singleton ..> CompositionRoot : ICompositionRoot Root
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Singleton</summary><blockquote>

```c#
partial class Singleton
{
  private readonly System.IDisposable[] _disposableSingletonsM09D22di;
  private Pure.DI.Benchmarks.Model.Service1 _singletonM09D22di23;
  
  public Singleton()
  {
    _disposableSingletonsM09D22di = new System.IDisposable[0];
  }
  
  internal Singleton(Singleton parent)
  {
    _disposableSingletonsM09D22di = new System.IDisposable[0];
    lock (parent._disposableSingletonsM09D22di)
    {
      _singletonM09D22di23 = parent._singletonM09D22di23;
    }
  }
  
  #region Composition Roots
  public Pure.DI.Benchmarks.Model.ICompositionRoot Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di83 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di82 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di81 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di80 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di79 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di78 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di77 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di76 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di75 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di74 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di73 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di72 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di71 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di70 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di69 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service3 transientM09D22di68 = new Pure.DI.Benchmarks.Model.Service3();
      Pure.DI.Benchmarks.Model.Service2 transientM09D22di67 = new Pure.DI.Benchmarks.Model.Service2(transientM09D22di69, transientM09D22di70, transientM09D22di71, transientM09D22di72, transientM09D22di73);
      Pure.DI.Benchmarks.Model.Service2 transientM09D22di66 = new Pure.DI.Benchmarks.Model.Service2(transientM09D22di74, transientM09D22di75, transientM09D22di76, transientM09D22di77, transientM09D22di78);
      Pure.DI.Benchmarks.Model.Service2 transientM09D22di65 = new Pure.DI.Benchmarks.Model.Service2(transientM09D22di79, transientM09D22di80, transientM09D22di81, transientM09D22di82, transientM09D22di83);
      if (global::System.Object.ReferenceEquals(_singletonM09D22di23, null))
      {
          Pure.DI.Benchmarks.Model.Service3 transientM09D22di89 = new Pure.DI.Benchmarks.Model.Service3();
          Pure.DI.Benchmarks.Model.Service3 transientM09D22di88 = new Pure.DI.Benchmarks.Model.Service3();
          Pure.DI.Benchmarks.Model.Service3 transientM09D22di87 = new Pure.DI.Benchmarks.Model.Service3();
          Pure.DI.Benchmarks.Model.Service3 transientM09D22di86 = new Pure.DI.Benchmarks.Model.Service3();
          Pure.DI.Benchmarks.Model.Service3 transientM09D22di85 = new Pure.DI.Benchmarks.Model.Service3();
          Pure.DI.Benchmarks.Model.Service2 transientM09D22di84 = new Pure.DI.Benchmarks.Model.Service2(transientM09D22di85, transientM09D22di86, transientM09D22di87, transientM09D22di88, transientM09D22di89);
          _singletonM09D22di23 = new Pure.DI.Benchmarks.Model.Service1(transientM09D22di84);
      }
      Pure.DI.Benchmarks.Model.CompositionRoot transientM09D22di63 = new Pure.DI.Benchmarks.Model.CompositionRoot(_singletonM09D22di23, transientM09D22di65, transientM09D22di66, transientM09D22di67, transientM09D22di68);
      return transientM09D22di63;
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
    return ResolverM09D22di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM09D22di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM09D22di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM09D22di;
    do {
      ref var pair = ref _bucketsM09D22di[index];
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
    var index = (int)(_bucketSizeM09D22di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM09D22di;
    do {
      ref var pair = ref _bucketsM09D22di[index];
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
        "  class Singleton {\n" +
          "    +ICompositionRoot Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  CompositionRoot --|> ICompositionRoot : \n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)\n" +
        "  }\n" +
        "  Service2 --|> IService2 : \n" +
        "  class Service2 {\n" +
          "    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3()\n" +
        "  }\n" +
        "  class ICompositionRoot {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService2 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService1 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService3 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  CompositionRoot o--  \"Singleton\" Service1 : IService1\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service1 *--  Service2 : IService2\n" +
        "  Singleton ..> CompositionRoot : ICompositionRoot Root";
  }
  
  private readonly static int _bucketSizeM09D22di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[] _bucketsM09D22di;
  
  static Singleton()
  {
    var valResolverM09D22di_0000 = new ResolverM09D22di_0000();
    ResolverM09D22di<Pure.DI.Benchmarks.Model.ICompositionRoot>.Value = valResolverM09D22di_0000;
    _bucketsM09D22di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>.Create(
      1,
      out _bucketSizeM09D22di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>(typeof(Pure.DI.Benchmarks.Model.ICompositionRoot), valResolverM09D22di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM09D22di<T>: global::Pure.DI.IResolver<Singleton, T>
  {
    public static global::Pure.DI.IResolver<Singleton, T> Value = new ResolverM09D22di<T>();
    
    public T Resolve(Singleton composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Singleton composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM09D22di_0000: global::Pure.DI.IResolver<Singleton, Pure.DI.Benchmarks.Model.ICompositionRoot>
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot Resolve(Singleton composition)
    {
      return composition.Root;
    }
    
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot ResolveByTag(Singleton composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.Benchmarks.Model.ICompositionRoot.");
    }
  }
  #endregion
}
```

</blockquote></details>

