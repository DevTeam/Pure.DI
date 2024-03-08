#### Tag Type

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/TagTypeScenario.cs)

`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.

```c#
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

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
  AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency) 
  AbcDependency --|> IDependency : 
  class AbcDependency {
    +AbcDependency()
  }
  XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency) 
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
  Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency)  IDependency
  Service o--  "Singleton" XyzDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency)  IDependency
  Service *--  AbcDependency : IDependency
  Composition ..> Service : IService Root
  Composition ..> XyzDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency) IDependency XyzRoot
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
/// <para>
/// Composition roots:<br/>
/// <list type="table">
/// <listheader>
/// <term>Root</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.TagTypeScenario.Service"/> Root
/// </term>
/// <description>
/// </description>
/// </item>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency"/> XyzRoot
/// </term>
/// <description>
/// "XyzRoot" is root name, typeof(XyzDependency) is tag
/// </description>
/// </item>
/// </list>
/// </para>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.TagTypeScenario.Service"/> using the composition root <see cref="Root"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.Root;
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNrNVUtuwjAQvYrldRcUFlB2QKjEroJU6sIb40xTtyRGtkGiiDtwl256HW7SxA7FScxHrUS7mYzn43nzMtasMRMR4C5mM6pUwGksaUIkSc0ZDUQyF4prLlJEFo1Gu5_7cq3ZH01ALjkDNBZCO-YA5pBGkLIVelq9l50oRGNQYraE3fYz3G0_jLlj5N3ZMDF9BaZzvXWPNI1raTZgn2uTwtUckM7ED6IH6HTJdpBrvSlzejYRwUG2ByZ7iFxijKlnColnW_lhIW3RYTCy30dFYwhBaWXPfao4K_SQxjnUCYOUSi6ssQTEIkW_A3iYhPod7izUK3d8TGXz8D-YKgFxmLK91mG6vdZzvb3uX8fxLt2IEteuw61c2G1Nl6ToW70t5tbvbZ70tnxtWETVP3JA1bLtDOhUaUmLpzIspO8mf3OXX1PiteeyizxDep1H5vLjYhRleARPeBrPQIuUYM-UXWfOj4G9mNBKfnVHNM1vszIb88qU17bGmfS_YunIGsM3OAGZUB5lO3NNsH6BBAjuEhxR-UbwBm--ABBPelY">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// </summary>
/// <seealso cref="Pure.DI.DI.Setup"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D08di;
  private readonly object _lockM03D08di;
  private Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency _singletonM03D08di35_XyzDependency;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D08di = this;
    _lockM03D08di = new object();
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D08di = baseComposition._rootM03D08di;
    _lockM03D08di = _rootM03D08di._lockM03D08di;
  }
  
  #region Composition Roots
  /// <summary>
  /// "XyzRoot" is root name, typeof(XyzDependency) is tag
  /// </summary>
  public Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency XyzRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (ReferenceEquals(_rootM03D08di._singletonM03D08di35_XyzDependency, null))
      {
          lock (_lockM03D08di)
          {
              if (ReferenceEquals(_rootM03D08di._singletonM03D08di35_XyzDependency, null))
              {
                  _singletonM03D08di35_XyzDependency = new Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency();
                  _rootM03D08di._singletonM03D08di35_XyzDependency = _singletonM03D08di35_XyzDependency;
              }
          }
      }
      return _rootM03D08di._singletonM03D08di35_XyzDependency;
    }
  }
  
  public Pure.DI.UsageTests.Basics.TagTypeScenario.IService Root
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (ReferenceEquals(_rootM03D08di._singletonM03D08di35_XyzDependency, null))
      {
          lock (_lockM03D08di)
          {
              if (ReferenceEquals(_rootM03D08di._singletonM03D08di35_XyzDependency, null))
              {
                  _singletonM03D08di35_XyzDependency = new Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency();
                  _rootM03D08di._singletonM03D08di35_XyzDependency = _singletonM03D08di35_XyzDependency;
              }
          }
      }
      return new Pure.DI.UsageTests.Basics.TagTypeScenario.Service(new Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency(), _rootM03D08di._singletonM03D08di35_XyzDependency, new Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency());
    }
  }
  #endregion
  
  #region API
  /// <summary>
  /// Resolves the composition root.
  /// </summary>
  /// <typeparam name="T">The type of the composition root.</typeparam>
  /// <returns>A composition root.</returns>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM03D08di<T>.Value.Resolve(this);
  }
  
  /// <summary>
  /// Resolves the composition root by tag.
  /// </summary>
  /// <typeparam name="T">The type of the composition root.</typeparam>
  /// <param name="tag">The tag of a composition root.</param>
  /// <returns>A composition root.</returns>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM03D08di<T>.Value.ResolveByTag(this, tag);
  }
  
  /// <summary>
  /// Resolves the composition root.
  /// </summary>
  /// <param name="type">The type of the composition root.</param>
  /// <returns>A composition root.</returns>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D08di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D08di;
    do {
      ref var pair = ref _bucketsM03D08di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  /// <summary>
  /// Resolves the composition root by tag.
  /// </summary>
  /// <param name="type">The type of the composition root.</param>
  /// <param name="tag">The tag of a composition root.</param>
  /// <returns>A composition root.</returns>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D08di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D08di;
    do {
      ref var pair = ref _bucketsM03D08di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  #endregion
  
  /// <summary>
  /// This method provides a class diagram in mermaid format. To see this diagram, simply call the method and copy the text to this site https://mermaid.live/.
  /// </summary>
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
        "  AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency) \n" +
        "  AbcDependency --|> IDependency : \n" +
        "  class AbcDependency {\n" +
          "    +AbcDependency()\n" +
        "  }\n" +
        "  XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency) \n" +
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
        "  Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.AbcDependency)  IDependency\n" +
        "  Service o--  \"Singleton\" XyzDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency)  IDependency\n" +
        "  Service *--  AbcDependency : IDependency\n" +
        "  Composition ..> Service : IService Root\n" +
        "  Composition ..> XyzDependency : typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency) IDependency XyzRoot";
  }
  
  private readonly static int _bucketSizeM03D08di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D08di;
  
  static Composition()
  {
    var valResolverM03D08di_0000 = new ResolverM03D08di_0000();
    ResolverM03D08di<Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency>.Value = valResolverM03D08di_0000;
    var valResolverM03D08di_0001 = new ResolverM03D08di_0001();
    ResolverM03D08di<Pure.DI.UsageTests.Basics.TagTypeScenario.IService>.Value = valResolverM03D08di_0001;
    _bucketsM03D08di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM03D08di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency), valResolverM03D08di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.IService), valResolverM03D08di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM03D08di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D08di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D08di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency>
  {
    public Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency.");
    }
    
    public Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, typeof(Pure.DI.UsageTests.Basics.TagTypeScenario.XyzDependency))) return composition.XyzRoot;
      switch (tag)
      {
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.TagTypeScenario.IDependency.");
    }
  }
  
  private sealed class ResolverM03D08di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.TagTypeScenario.IService>
  {
    public Pure.DI.UsageTests.Basics.TagTypeScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Basics.TagTypeScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.TagTypeScenario.IService.");
    }
  }
  #endregion
}
```

</blockquote></details>

