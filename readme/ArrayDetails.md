## Array details

Creating an object graph of 27 transient objects, including 4 transient array objects.

### Class diagram
```mermaid
classDiagram
  class Array {
    +ICompositionRoot Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  class ArrayᐸIService3ᐳ
  CompositionRoot --|> ICompositionRoot : 
  class CompositionRoot {
    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)
  }
  Service1 --|> IService1 : 
  class Service1 {
    +Service1(IService2 service2)
  }
  Service2Array --|> IService2 : 
  class Service2Array {
    +Service2Array(ArrayᐸIService3ᐳ services)
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
  ArrayᐸIService3ᐳ *--  Service3 : IService3
  ArrayᐸIService3ᐳ *--  Service3v2 : 2  IService3
  ArrayᐸIService3ᐳ *--  Service3v3 : 3  IService3
  ArrayᐸIService3ᐳ *--  Service3v4 : 4  IService3
  CompositionRoot *--  Service1 : IService1
  CompositionRoot *--  Service2Array : IService2
  CompositionRoot *--  Service2Array : IService2
  CompositionRoot *--  Service2Array : IService2
  CompositionRoot *--  Service3 : IService3
  Service1 *--  Service2Array : IService2
  Service2Array *--  ArrayᐸIService3ᐳ : ArrayᐸIService3ᐳ
  Array ..> CompositionRoot : ICompositionRoot Root
```

### Generated code

<details>
<summary>Pure.DI-generated partial class Array</summary><blockquote>

```c#
partial class Array
{
  private readonly global::System.IDisposable[] _disposableSingletonsM10D11di;
  
  public Array()
  {
    _disposableSingletonsM10D11di = new global::System.IDisposable[0];
  }
  
  internal Array(Array parent)
  {
    _disposableSingletonsM10D11di = new global::System.IDisposable[0];
  }
  
  #region Composition Roots
  public Pure.DI.Benchmarks.Model.ICompositionRoot Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      var transientM10D11di26 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM10D11di25 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM10D11di24 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM10D11di23 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM10D11di22 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM10D11di23,
          transientM10D11di24,
          transientM10D11di25,
          transientM10D11di26
      };
      var transientM10D11di21 = new Pure.DI.Benchmarks.Model.Service2Array(transientM10D11di22);
      var transientM10D11di20 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM10D11di19 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM10D11di18 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM10D11di17 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM10D11di16 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM10D11di17,
          transientM10D11di18,
          transientM10D11di19,
          transientM10D11di20
      };
      var transientM10D11di15 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM10D11di14 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM10D11di13 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM10D11di12 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM10D11di11 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM10D11di12,
          transientM10D11di13,
          transientM10D11di14,
          transientM10D11di15
      };
      var transientM10D11di10 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM10D11di9 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM10D11di8 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM10D11di7 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM10D11di6 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM10D11di7,
          transientM10D11di8,
          transientM10D11di9,
          transientM10D11di10
      };
      var transientM10D11di5 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM10D11di4 = new Pure.DI.Benchmarks.Model.Service2Array(transientM10D11di6);
      var transientM10D11di3 = new Pure.DI.Benchmarks.Model.Service2Array(transientM10D11di11);
      var transientM10D11di2 = new Pure.DI.Benchmarks.Model.Service2Array(transientM10D11di16);
      var transientM10D11di1 = new Pure.DI.Benchmarks.Model.Service1(transientM10D11di21);
      var transientM10D11di0 = new Pure.DI.Benchmarks.Model.CompositionRoot(transientM10D11di1, transientM10D11di2, transientM10D11di3, transientM10D11di4, transientM10D11di5);
      return transientM10D11di0;
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
    return ResolverM10D11di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM10D11di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM10D11di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM10D11di;
    do {
      ref var pair = ref _bucketsM10D11di[index];
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
    var index = (int)(_bucketSizeM10D11di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM10D11di;
    do {
      ref var pair = ref _bucketsM10D11di[index];
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
        "  class Array {\n" +
          "    +ICompositionRoot Root\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class ArrayᐸIService3ᐳ\n" +
        "  CompositionRoot --|> ICompositionRoot : \n" +
        "  class CompositionRoot {\n" +
          "    +CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)\n" +
        "  }\n" +
        "  Service1 --|> IService1 : \n" +
        "  class Service1 {\n" +
          "    +Service1(IService2 service2)\n" +
        "  }\n" +
        "  Service2Array --|> IService2 : \n" +
        "  class Service2Array {\n" +
          "    +Service2Array(ArrayᐸIService3ᐳ services)\n" +
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
        "  ArrayᐸIService3ᐳ *--  Service3 : IService3\n" +
        "  ArrayᐸIService3ᐳ *--  Service3v2 : 2  IService3\n" +
        "  ArrayᐸIService3ᐳ *--  Service3v3 : 3  IService3\n" +
        "  ArrayᐸIService3ᐳ *--  Service3v4 : 4  IService3\n" +
        "  CompositionRoot *--  Service1 : IService1\n" +
        "  CompositionRoot *--  Service2Array : IService2\n" +
        "  CompositionRoot *--  Service2Array : IService2\n" +
        "  CompositionRoot *--  Service2Array : IService2\n" +
        "  CompositionRoot *--  Service3 : IService3\n" +
        "  Service1 *--  Service2Array : IService2\n" +
        "  Service2Array *--  ArrayᐸIService3ᐳ : ArrayᐸIService3ᐳ\n" +
        "  Array ..> CompositionRoot : ICompositionRoot Root";
  }
  
  private readonly static int _bucketSizeM10D11di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>[] _bucketsM10D11di;
  
  static Array()
  {
    var valResolverM10D11di_0000 = new ResolverM10D11di_0000();
    ResolverM10D11di<Pure.DI.Benchmarks.Model.ICompositionRoot>.Value = valResolverM10D11di_0000;
    _bucketsM10D11di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Array, object>>.Create(
      1,
      out _bucketSizeM10D11di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>(typeof(Pure.DI.Benchmarks.Model.ICompositionRoot), valResolverM10D11di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM10D11di<T>: global::Pure.DI.IResolver<Array, T>
  {
    public static global::Pure.DI.IResolver<Array, T> Value = new ResolverM10D11di<T>();
    
    public T Resolve(Array composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Array composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM10D11di_0000: global::Pure.DI.IResolver<Array, Pure.DI.Benchmarks.Model.ICompositionRoot>
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot Resolve(Array composition)
    {
      return composition.Root;
    }
    
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.Benchmarks.Model.ICompositionRoot ResolveByTag(Array composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.Benchmarks.Model.ICompositionRoot.");
    }
  }
  #endregion
}
```

</blockquote></details>

