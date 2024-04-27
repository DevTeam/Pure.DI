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
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root")

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
  private readonly Composition _rootM04D27di;
  private readonly object _lockM04D27di;
  private XyzDependency _singletonM04D27di37_XyzDependency;

  public Composition()
  {
    _rootM04D27di = this;
    _lockM04D27di = new object();
  }

  internal Composition(Composition baseComposition)
  {
    _rootM04D27di = baseComposition._rootM04D27di;
    _lockM04D27di = _rootM04D27di._lockM04D27di;
  }

  public IDependency XyzRoot
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      if (_rootM04D27di._singletonM04D27di37_XyzDependency == null)
      {
          lock (_lockM04D27di)
          {
              if (_rootM04D27di._singletonM04D27di37_XyzDependency == null)
              {
                  _singletonM04D27di37_XyzDependency = new XyzDependency();
                  _rootM04D27di._singletonM04D27di37_XyzDependency = _singletonM04D27di37_XyzDependency;
              }
          }
      }
      return _rootM04D27di._singletonM04D27di37_XyzDependency;
    }
  }

  public IService Root
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      if (_rootM04D27di._singletonM04D27di37_XyzDependency == null)
      {
          lock (_lockM04D27di)
          {
              if (_rootM04D27di._singletonM04D27di37_XyzDependency == null)
              {
                  _singletonM04D27di37_XyzDependency = new XyzDependency();
                  _rootM04D27di._singletonM04D27di37_XyzDependency = _singletonM04D27di37_XyzDependency;
              }
          }
      }
      return new Service(new AbcDependency(), _rootM04D27di._singletonM04D27di37_XyzDependency, new AbcDependency());
    }
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return ResolverM04D27di<T>.Value.Resolve(this);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM04D27di<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.Resolve(this) : ResolveM04D27di(type, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : ResolveM04D27di(type, tag, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, object? tag, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
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
        "  AbcDependency --|> IDependency : typeof(AbcDependency) \n" +
        "  AbcDependency --|> IDependency : \n" +
        "  class AbcDependency {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependency --|> IDependency : typeof(XyzDependency) \n" +
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
        "  Service *--  AbcDependency : typeof(AbcDependency)  IDependency\n" +
        "  Service o--  \"Singleton\" XyzDependency : typeof(XyzDependency)  IDependency\n" +
        "  Service *--  AbcDependency : IDependency\n" +
        "  Composition ..> Service : IService Root\n" +
        "  Composition ..> XyzDependency : IDependency XyzRoot";
  }

  private readonly static int _bucketSizeM04D27di;
  private readonly static Pair<Type, IResolver<Composition, object>>[] _bucketsM04D27di;

  static Composition()
  {
    var valResolverM04D27di_0000 = new ResolverM04D27di_0000();
    ResolverM04D27di<IDependency>.Value = valResolverM04D27di_0000;
    var valResolverM04D27di_0001 = new ResolverM04D27di_0001();
    ResolverM04D27di<IService>.Value = valResolverM04D27di_0001;
    _bucketsM04D27di = Buckets<Type, IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM04D27di,
      new Pair<Type, IResolver<Composition, object>>[2]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof(IDependency), valResolverM04D27di_0000)
        ,new Pair<Type, IResolver<Composition, object>>(typeof(IService), valResolverM04D27di_0001)
      });
  }

  private sealed class ResolverM04D27di<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new ResolverM04D27di<T>();

    public T Resolve(Composition composite)
    {
      throw new InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }

    public T ResolveByTag(Composition composite, object tag)
    {
      throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }

  private sealed class ResolverM04D27di_0000: IResolver<Composition, IDependency>
  {
    public IDependency Resolve(Composition composition)
    {
      throw new InvalidOperationException($"Cannot resolve composition root of type IDependency.");
    }

    public IDependency ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, typeof(XyzDependency))) return composition.XyzRoot;
      switch (tag)
      {
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type IDependency.");
      }
    }
  }

  private sealed class ResolverM04D27di_0001: IResolver<Composition, IService>
  {
    public IService Resolve(Composition composition)
    {
      return composition.Root;
    }

    public IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type IService.");
      }
    }
  }
}
```

</blockquote></details>

