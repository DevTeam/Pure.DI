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
  private readonly System.IDisposable[] _disposableSingletonsM09D23di;
  
  public Array()
  {
    _disposableSingletonsM09D23di = new System.IDisposable[0];
  }
  
  internal Array(Array parent)
  {
    _disposableSingletonsM09D23di = new System.IDisposable[0];
  }
  
  #region Composition Roots
  public Pure.DI.Benchmarks.Model.ICompositionRoot Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      var transientM09D23di26 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM09D23di25 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM09D23di24 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM09D23di23 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM09D23di22 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM09D23di23,
          transientM09D23di24,
          transientM09D23di25,
          transientM09D23di26
      };
      var transientM09D23di21 = new Pure.DI.Benchmarks.Model.Service2Array(transientM09D23di22);
      var transientM09D23di20 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM09D23di19 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM09D23di18 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM09D23di17 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM09D23di16 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM09D23di17,
          transientM09D23di18,
          transientM09D23di19,
          transientM09D23di20
      };
      var transientM09D23di15 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM09D23di14 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM09D23di13 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM09D23di12 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM09D23di11 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM09D23di12,
          transientM09D23di13,
          transientM09D23di14,
          transientM09D23di15
      };
      var transientM09D23di10 = new Pure.DI.Benchmarks.Model.Service3v4();
      var transientM09D23di9 = new Pure.DI.Benchmarks.Model.Service3v3();
      var transientM09D23di8 = new Pure.DI.Benchmarks.Model.Service3v2();
      var transientM09D23di7 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM09D23di6 = new Pure.DI.Benchmarks.Model.IService3[4]
      {
          transientM09D23di7,
          transientM09D23di8,
          transientM09D23di9,
          transientM09D23di10
      };
      var transientM09D23di5 = new Pure.DI.Benchmarks.Model.Service3();
      var transientM09D23di4 = new Pure.DI.Benchmarks.Model.Service2Array(transientM09D23di6);
      var transientM09D23di3 = new Pure.DI.Benchmarks.Model.Service2Array(transientM09D23di11);
      var transientM09D23di2 = new Pure.DI.Benchmarks.Model.Service2Array(transientM09D23di16);
      var transientM09D23di1 = new Pure.DI.Benchmarks.Model.Service1(transientM09D23di21);
      var transientM09D23di0 = new Pure.DI.Benchmarks.Model.CompositionRoot(transientM09D23di1, transientM09D23di2, transientM09D23di3, transientM09D23di4, transientM09D23di5);
      return transientM09D23di0;
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
    return ResolverM09D23di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM09D23di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM09D23di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM09D23di;
    do {
      ref var pair = ref _bucketsM09D23di[index];
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
    var index = (int)(_bucketSizeM09D23di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM09D23di;
    do {
      ref var pair = ref _bucketsM09D23di[index];
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
  
  private readonly static int _bucketSizeM09D23di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>[] _bucketsM09D23di;
  
  static Array()
  {
    var valResolverM09D23di_0000 = new ResolverM09D23di_0000();
    ResolverM09D23di<Pure.DI.Benchmarks.Model.ICompositionRoot>.Value = valResolverM09D23di_0000;
    _bucketsM09D23di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Array, object>>.Create(
      1,
      out _bucketSizeM09D23di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Array, object>>(typeof(Pure.DI.Benchmarks.Model.ICompositionRoot), valResolverM09D23di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM09D23di<T>: global::Pure.DI.IResolver<Array, T>
  {
    public static global::Pure.DI.IResolver<Array, T> Value = new ResolverM09D23di<T>();
    
    public T Resolve(Array composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Array composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM09D23di_0000: global::Pure.DI.IResolver<Array, Pure.DI.Benchmarks.Model.ICompositionRoot>
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

