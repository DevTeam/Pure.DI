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
  private readonly System.IDisposable[] _disposableSingletonsM08D07di;
  private Pure.DI.Benchmarks.Model.Service1 _singletonM08D07di23;
  
  public Singleton()
  {
    _disposableSingletonsM08D07di = new System.IDisposable[0];
  }
  
  internal Singleton(Singleton parent)
  {
    _disposableSingletonsM08D07di = new System.IDisposable[0];
    lock (parent._disposableSingletonsM08D07di)
    {
      _singletonM08D07di23 = parent._singletonM08D07di23;
    }
  }
  
  #region Composition Roots
  public Pure.DI.Benchmarks.Model.ICompositionRoot Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      if (global::System.Object.ReferenceEquals(_singletonM08D07di23, null))
      {
          var transientM08D07di21 = new Pure.DI.Benchmarks.Model.Service3();
          var transientM08D07di22 = new Pure.DI.Benchmarks.Model.Service3();
          var transientM08D07di23 = new Pure.DI.Benchmarks.Model.Service3();
          var transientM08D07di24 = new Pure.DI.Benchmarks.Model.Service3();
          var transientM08D07di25 = new Pure.DI.Benchmarks.Model.Service3();
          var transientM08D07di20 = new Pure.DI.Benchmarks.Model.Service2(transientM08D07di25, transientM08D07di24, transientM08D07di23, transientM08D07di22, transientM08D07di21);
          _singletonM08D07di23 = new Pure.DI.Benchmarks.Model.Service1(transientM08D07di20);
      }
      var transientM08D07di15 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di16 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di17 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di18 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di19 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di1 = new Pure.DI.Benchmarks.Model.Service2(transientM08D07di19, transientM08D07di18, transientM08D07di17, transientM08D07di16, transientM08D07di15);
      var transientM08D07di10 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di11 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di12 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di13 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di14 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di2 = new Pure.DI.Benchmarks.Model.Service2(transientM08D07di14, transientM08D07di13, transientM08D07di12, transientM08D07di11, transientM08D07di10);
      var transientM08D07di5 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di6 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di7 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di8 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di9 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di3 = new Pure.DI.Benchmarks.Model.Service2(transientM08D07di9, transientM08D07di8, transientM08D07di7, transientM08D07di6, transientM08D07di5);
      var transientM08D07di4 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM08D07di0 = new Pure.DI.Benchmarks.Model.CompositionRoot(_singletonM08D07di23, transientM08D07di3, transientM08D07di2, transientM08D07di1, transientM08D07di4);
      return transientM08D07di0;
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
    return ResolverM08D07di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM08D07di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM08D07di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM08D07di;
    do {
      ref var pair = ref _bucketsM08D07di[index];
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
    var index = (int)(_bucketSizeM08D07di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM08D07di;
    do {
      ref var pair = ref _bucketsM08D07di[index];
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
  
  private readonly static int _bucketSizeM08D07di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[] _bucketsM08D07di;
  
  static Singleton()
  {
    var valResolverM08D07di_0000 = new ResolverM08D07di_0000();
    ResolverM08D07di<Pure.DI.Benchmarks.Model.ICompositionRoot>.Value = valResolverM08D07di_0000;
    _bucketsM08D07di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>.Create(
      1,
      out _bucketSizeM08D07di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>(typeof(Pure.DI.Benchmarks.Model.ICompositionRoot), valResolverM08D07di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM08D07di<T>: global::Pure.DI.IResolver<Singleton, T>
  {
    public static global::Pure.DI.IResolver<Singleton, T> Value = new ResolverM08D07di<T>();
    
    public T Resolve(Singleton composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Singleton composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM08D07di_0000: global::Pure.DI.IResolver<Singleton, Pure.DI.Benchmarks.Model.ICompositionRoot>
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

