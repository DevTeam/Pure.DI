## Singleton details

Creating an object graph of 20 transition objects plus 1 singleton with an additional 6 transition objects .

### Class diagram
```mermaid
classDiagram
  class Singleton {
    +CompositionRoot TestPureDIByCR()
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class CompositionRoot {
    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)
  }
  Service1 --|> IService1 : 
  class Service1 {
    +Service1(IService2 service2)
  }
  Service2 --|> IService2 : 
  class Service2 {
    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)
  }
  Service3 --|> IService3 : 
  class Service3 {
    +Service3(IService4 service41, IService4 service42)
  }
  Service4 --|> IService4 : 
  class Service4 {
    +Service4()
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
  class IService4 {
    <<abstract>>
  }
  CompositionRoot o--  "Scoped" Service1 : IService1
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service2 : IService2
  CompositionRoot *--  Service3 : IService3
  CompositionRoot o--  "Scoped" Service4 : IService4
  CompositionRoot o--  "Scoped" Service4 : IService4
  Service1 *--  Service2 : IService2
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service2 *--  Service3 : IService3
  Service3 o--  "Scoped" Service4 : IService4
  Service3 o--  "Scoped" Service4 : IService4
  Singleton ..> CompositionRoot : CompositionRoot TestPureDIByCR()
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Singleton</summary><blockquote>

```c#
partial class Singleton
{
  private readonly Singleton _rootM04D26di;
  private Pure.DI.Benchmarks.Model.Service1 _scopedM04D26di37_Service1;
  private Pure.DI.Benchmarks.Model.Service4 _scopedM04D26di40_Service4;
  
  public Singleton()
  {
    _rootM04D26di = this;
  }
  
  internal Singleton(Singleton baseComposition)
  {
    _rootM04D26di = baseComposition._rootM04D26di;
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public partial Pure.DI.Benchmarks.Model.CompositionRoot TestPureDIByCR()
  {
    if (_scopedM04D26di40_Service4 == null)
    {
        _scopedM04D26di40_Service4 = new Pure.DI.Benchmarks.Model.Service4();
    }
    if (_scopedM04D26di37_Service1 == null)
    {
        _scopedM04D26di37_Service1 = new Pure.DI.Benchmarks.Model.Service1(new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4)));
    }
    return new Pure.DI.Benchmarks.Model.CompositionRoot(_scopedM04D26di37_Service1, new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4)), new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4)), new Pure.DI.Benchmarks.Model.Service2(new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4)), new Pure.DI.Benchmarks.Model.Service3(_scopedM04D26di40_Service4, _scopedM04D26di40_Service4), _scopedM04D26di40_Service4, _scopedM04D26di40_Service4);
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
        "  class Singleton {\n" +
          "    +CompositionRoot TestPureDIByCR()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3, IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service2 --|> IService2 : \n" +
        "  class Service2 {\n" +
          "    +Service2(IService3 service31, IService3 service32, IService3 service33, IService3 service34, IService3 service35)\n" +
        "  }\n" +
        "  Service3 --|> IService3 : \n" +
        "  class Service3 {\n" +
          "    +Service3(IService4 service41, IService4 service42)\n" +
        "  }\n" +
        "  Service4 --|> IService4 : \n" +
        "  class Service4 {\n" +
          "    +Service4()\n" +
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
        "  class IService4 {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  CompositionRoot o--  \"Scoped\" Service1 : IService1\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service2 : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  CompositionRoot o--  \"Scoped\" Service4 : IService4\n" +
        "  CompositionRoot o--  \"Scoped\" Service4 : IService4\n" +
        "  Service1 *--  Service2 : IService2\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service2 *--  Service3 : IService3\n" +
        "  Service3 o--  \"Scoped\" Service4 : IService4\n" +
        "  Service3 o--  \"Scoped\" Service4 : IService4\n" +
        "  Singleton ..> CompositionRoot : CompositionRoot TestPureDIByCR()";
  }
  
  private readonly static int _bucketSizeM04D26di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[] _bucketsM04D26di;
  
  static Singleton()
  {
    var valResolverM04D26di_0000 = new ResolverM04D26di_0000();
    ResolverM04D26di<Pure.DI.Benchmarks.Model.CompositionRoot>.Value = valResolverM04D26di_0000;
    _bucketsM04D26di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>.Create(
      1,
      out _bucketSizeM04D26di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Singleton, object>>(typeof(Pure.DI.Benchmarks.Model.CompositionRoot), valResolverM04D26di_0000)
      });
  }
  
  private sealed class ResolverM04D26di<T>: global::Pure.DI.IResolver<Singleton, T>
  {
    public static global::Pure.DI.IResolver<Singleton, T> Value = new ResolverM04D26di<T>();
    
    public T Resolve(Singleton composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Singleton composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM04D26di_0000: global::Pure.DI.IResolver<Singleton, Pure.DI.Benchmarks.Model.CompositionRoot>
  {
    public Pure.DI.Benchmarks.Model.CompositionRoot Resolve(Singleton composition)
    {
      return composition.TestPureDIByCR();
    }
    
    public Pure.DI.Benchmarks.Model.CompositionRoot ResolveByTag(Singleton composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.TestPureDIByCR();
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.Benchmarks.Model.CompositionRoot.");
      }
    }
  }
}
```

</blockquote></details>

