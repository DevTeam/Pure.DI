## Singleton details

Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .

### Class diagram
```mermaid
classDiagram
  class Singleton {
    +CompositionRoot PureDIByCR
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class CompositionRoot {
    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)
  }
  Service2 --|> IService2 : 
  class Service2 {
    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
  }
  Service3 --|> IService3 : 
  class Service3 {
    +Service3(IService4 service41, IService4 service42)
  }
  Service1 --|> IService1 : 
  class Service1 {
    +Service1(IService2 service2)
  }
  Service4 --|> IService4 : 
  class Service4 {
    +Service4()
  }
  class IService2 {
    <<abstract>>
  }
  class IService3 {
    <<abstract>>
  }
  class IService1 {
    <<abstract>>
  }
  class IService4 {
    <<abstract>>
  }
  CompositionRoot o--  "Singleton" Service1 : IService1
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service3 : IService3
  CompositionRoot o--  "Singleton" Service4 : IService4
  CompositionRoot o--  "Singleton" Service4 : IService4
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service3 o--  "Singleton" Service4 : IService4
  Service3 o--  "Singleton" Service4 : IService4
  Service1 *--  Service2 : IService2
  Singleton ..> CompositionRoot : CompositionRoot PureDIByCR
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Singleton</summary><blockquote>

```c#
partial class Singleton
{
  private readonly global::System.IDisposable[] _disposableSingletonsM12D27di;
  private Pure.DI.Benchmarks.Model.Service1 _singletonM12D27di22;
  private Pure.DI.Benchmarks.Model.Service4 _singletonM12D27di25;
  
  public Singleton()
  {
    _disposableSingletonsM12D27di = new global::System.IDisposable[0];
  }
  
  internal Singleton(Singleton parent)
  {
    _disposableSingletonsM12D27di = new global::System.IDisposable[0];
    lock (parent._disposableSingletonsM12D27di)
    {
      _singletonM12D27di22 = parent._singletonM12D27di22;
      _singletonM12D27di25 = parent._singletonM12D27di25;
    }
  }
  
  #region Composition Roots
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public partial Pure.DI.Benchmarks.Model.CompositionRoot PureDIByCR()
  {
    if (object.ReferenceEquals(_singletonM12D27di25, null))
    {
        _singletonM12D27di25 = new Pure.DI.Benchmarks.Model.Service4();
    }
    if (object.ReferenceEquals(_singletonM12D27di22, null))
    {
        _singletonM12D27di22 = new Pure.DI.Benchmarks.Model.Service1(new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25)));
    }
    return new Pure.DI.Benchmarks.Model.CompositionRoot(_singletonM12D27di22, new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25)), new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25)), new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25)), new Pure.DI.Benchmarks.Model.Service3(_singletonM12D27di25, _singletonM12D27di25), _singletonM12D27di25, _singletonM12D27di25);
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM12D27di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM12D27di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM12D27di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM12D27di;
    do {
      ref var pair = ref _bucketsM12D27di[index];
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
    var index = (int)(_bucketSizeM12D27di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM12D27di;
    do {
      ref var pair = ref _bucketsM12D27di[index];
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
          "    +CompositionRoot PureDIByCR\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service2 --|> IService2 : \n" +
        "  class Service2 {\n" +
          "    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service4 --|> IService4 : \n" +
        "  class Service4 {\n" +
          "    +Service4()\n" +
        "  }\n" +
        "  class IService2 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService3 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService1 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService4 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  CompositionRoot o--  \"Singleton\" Service1 : IService1\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  CompositionRoot o--  \"Singleton\" Service4 : IService4\n" +
        "  CompositionRoot o--  \"Singleton\" Service4 : IService4\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service3 o--  \"Singleton\" Service4 : IService4\n" +
        "  Service3 o--  \"Singleton\" Service4 : IService4\n" +
        "  Service1 *--  Service2 : IService2\n" +
        "  Singleton ..> CompositionRoot : CompositionRoot PureDIByCR";
  }
  
  private readonly static int _bucketSizeM12D27di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[] _bucketsM12D27di;
  
  
  static Singleton()
  {
    var valResolverM12D27di_0000 = new ResolverM12D27di_0000();
    ResolverM12D27di<Pure.DI.Benchmarks.Model.CompositionRoot>.Value = valResolverM12D27di_0000;
    _bucketsM12D27di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>.Create(
      1,
      out _bucketSizeM12D27di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>(typeof(Pure.DI.Benchmarks.Model.CompositionRoot), valResolverM12D27di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM12D27di<T>: global::Pure.DI.IResolver<Singleton, T>
  {
    public static global::Pure.DI.IResolver<Singleton, T> Value = new ResolverM12D27di<T>();
    
    public T Resolve(Singleton composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Singleton composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM12D27di_0000: global::Pure.DI.IResolver<Singleton, Pure.DI.Benchmarks.Model.CompositionRoot>
  {
    public Pure.DI.Benchmarks.Model.CompositionRoot Resolve(Singleton composition)
    {
      return composition.PureDIByCR();
    }
    
    public Pure.DI.Benchmarks.Model.CompositionRoot ResolveByTag(Singleton composition, object tag)
    {
      if (Equals(tag, null)) return composition.PureDIByCR();;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.Benchmarks.Model.CompositionRoot.");
    }
  }
  #endregion
}
```

</blockquote></details>

