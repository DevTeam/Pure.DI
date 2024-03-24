#### Tag Type

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagTypeScenario.cs)

`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.

```c#
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}

DI.Setup(nameof(Composition))
    // Tag.Type here is the same as typeof(AbcDependency)
    .Bind<IDependency>(Tag.Type, default).To<AbcDependency>()
    // Tag.Type here is the same as typeof(XyzDependency)
    .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>().Root<IService>("Root")
    // "XyzRoot" is root name, typeof(XyzDependency) is tag
    .Root<IDependency>("XyzRoot", typeof(XyzDependency));

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService Root
    +IDependency XyzRoot
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency) 
  AbcDependency --|> IDependency : 
  class AbcDependency {
    +AbcDependency()
  }
  XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency) 
  class XyzDependency {
    +XyzDependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency)  IDependency
  Service o--  "Singleton" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency)  IDependency
  Service *--  AbcDependency : IDependency
  Composition ..> Service : IService Root
  Composition ..> XyzDependency : IDependency XyzRoot
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM03D24di;
  private readonly object _lockM03D24di;
  private Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency _singletonM03D24di37_XyzDependency;
  
  public Composition()
  {
    _rootM03D24di = this;
    _lockM03D24di = new object();
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM03D24di = baseComposition._rootM03D24di;
    _lockM03D24di = _rootM03D24di._lockM03D24di;
  }
  
  public Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency XyzRoot
  {
    get
    {
      if (ReferenceEquals(_rootM03D24di._singletonM03D24di37_XyzDependency, null))
      {
          lock (_lockM03D24di)
          {
              if (ReferenceEquals(_rootM03D24di._singletonM03D24di37_XyzDependency, null))
              {
                  _singletonM03D24di37_XyzDependency = new Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency();
                  _rootM03D24di._singletonM03D24di37_XyzDependency = _singletonM03D24di37_XyzDependency;
              }
          }
      }
      return _rootM03D24di._singletonM03D24di37_XyzDependency;
    }
  }
  
  public Pure.DI.UsageTests.Advanced.TagTypeScenario.IService Root
  {
    get
    {
      if (ReferenceEquals(_rootM03D24di._singletonM03D24di37_XyzDependency, null))
      {
          lock (_lockM03D24di)
          {
              if (ReferenceEquals(_rootM03D24di._singletonM03D24di37_XyzDependency, null))
              {
                  _singletonM03D24di37_XyzDependency = new Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency();
                  _rootM03D24di._singletonM03D24di37_XyzDependency = _singletonM03D24di37_XyzDependency;
              }
          }
      }
      return new Pure.DI.UsageTests.Advanced.TagTypeScenario.Service(new Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency(), _rootM03D24di._singletonM03D24di37_XyzDependency, new Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency());
    }
  }
  
  public T Resolve<T>()
  {
    return ResolverM03D24di<T>.Value.Resolve(this);
  }
  
  public T Resolve<T>(object? tag)
  {
    return ResolverM03D24di<T>.Value.ResolveByTag(this, tag);
  }
  
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D24di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D24di;
    do {
      ref var pair = ref _bucketsM03D24di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D24di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D24di;
    do {
      ref var pair = ref _bucketsM03D24di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IService Root\n" +
          "    +IDependency XyzRoot\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency) \n" +
        "  AbcDependency --|> IDependency : \n" +
        "  class AbcDependency {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency) \n" +
        "  class XyzDependency {\n" +
          "    +XyzDependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency)  IDependency\n" +
        "  Service o--  \"Singleton\" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency)  IDependency\n" +
        "  Service *--  AbcDependency : IDependency\n" +
        "  Composition ..> Service : IService Root\n" +
        "  Composition ..> XyzDependency : IDependency XyzRoot";
  }
  
  private readonly static int _bucketSizeM03D24di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D24di;
  
  static Composition()
  {
    var valResolverM03D24di_0000 = new ResolverM03D24di_0000();
    ResolverM03D24di<Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency>.Value = valResolverM03D24di_0000;
    var valResolverM03D24di_0001 = new ResolverM03D24di_0001();
    ResolverM03D24di<Pure.DI.UsageTests.Advanced.TagTypeScenario.IService>.Value = valResolverM03D24di_0001;
    _bucketsM03D24di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM03D24di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency), valResolverM03D24di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.IService), valResolverM03D24di_0001)
      });
  }
  
  private sealed class ResolverM03D24di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D24di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D24di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency>
  {
    public Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency.");
    }
    
    public Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency))) return composition.XyzRoot;
      switch (tag)
      {
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Advanced.TagTypeScenario.IDependency.");
    }
  }
  
  private sealed class ResolverM03D24di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Advanced.TagTypeScenario.IService>
  {
    public Pure.DI.UsageTests.Advanced.TagTypeScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Advanced.TagTypeScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Advanced.TagTypeScenario.IService.");
    }
  }
}
```

</blockquote></details>

