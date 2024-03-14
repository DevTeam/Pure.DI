#### Keyed service provider

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/KeyedServiceProviderScenario.cs)

```c#
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service([Tag("Dependency Key")] IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition: IKeyedServiceProvider
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IDependency>("Dependency Key").As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>("Service Key").To<Service>()
            .Root<IDependency>(tag: "Dependency Key")
            .Root<IService>(tag: "Service Key");

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}

var serviceProvider = new Composition();
var service = serviceProvider.GetRequiredKeyedService<IService>("Service Key");
var dependency = serviceProvider.GetRequiredKeyedService<IDependency>("Dependency Key");
service.Dependency.ShouldBe(dependency);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    -IDependency RootM03D14di0001
    -IService RootM03D14di0002
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object GetService(Type type)
    + object GetRequiredKeyedService(Type type, object? tag)
  }
  Dependency --|> IDependency : "Dependency Key" 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : "Service Key" 
  class Service {
    +Service(IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Service o--  "Singleton" Dependency : "Dependency Key"  IDependency
  Composition ..> Dependency : IDependency RootM03D14di0001<br/>provides "Dependency Key" IDependency
  Composition ..> Service : IService RootM03D14di0002<br/>provides "Service Key" IService
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
/// <para>
/// <b>Composition roots</b><br/>
/// <list type="table">
/// <listheader>
/// <term>Root</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>
/// Private composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency"/>. It can be resolved by <see cref="Resolve{T}(object)"/> method: <c>Resolve&lt;Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency&gt;("Dependency Key")</c>
/// </term>
/// <description>
/// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency"/>.
/// </description>
/// </item>
/// <item>
/// <term>
/// Private composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService"/>. It can be resolved by <see cref="Resolve{T}(object)"/> method: <c>Resolve&lt;Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService&gt;("Service Key")</c>
/// </term>
/// <description>
/// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Service"/>.
/// </description>
/// </item>
/// </list>
/// </para>
/// </summary>
/// <a href="https://mermaid.live/view#pako:eNqVlNFOwjAUhl-l6bWJA0xQ7tyGxhhvkMvelPUEq2ydXSEhhHfgXbzxdXgT13ZzXRkTb0669j-nX__TbocTwQBPcLKiRRFzupQ0JZJk5htFIs1FwRUXGSLrIBiHek2PhvFTDDlkDLJki2ZCqJdgFA9uGA-CYOCoXkFueAK-ZPgrCdEczaAQqw0cD9_z4-HLTN-aePenTCzeIVF6PHpAii5P0qwAPYKqUGzefJsDUmXoSZjB55pLYM-wBdaTHaF-inGsR45f1psmjiOTOkWuqWbqHhHszJUkBKOmQZ7c6U_YLHX5aZHq5pzncRUGpp7wSVyhi9GyzT0ea_N5ZLaob0dTeGQJI7oolKSV9dMqdlXq5ru8TJ0vXK-QNoRnyxUokZV2XNQ991S6sv_IhmZ_G8sWnNbsfXnmOAtJrm16LsWGMyi6QP7H4d2E8w_7PEH77tQV9N74CqcgU8pZ-S_aEazeIAWCJwQzKj8I3uP9D0VgkHc">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// <seealso cref="Pure.DI.DI.Setup"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind(object[])"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind{T}(object[])"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D14di;
  private readonly object _lockM03D14di;
  private Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency _singletonM03D14di34_Dependency;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D14di = this;
    _lockM03D14di = new object();
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D14di = baseComposition._rootM03D14di;
    _lockM03D14di = _rootM03D14di._lockM03D14di;
  }
  
  #region Composition Roots
  /// <summary>
  /// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency"/>.
  /// </summary>
  public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency RootM03D14di0001
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (ReferenceEquals(_rootM03D14di._singletonM03D14di34_Dependency, null))
      {
          lock (_lockM03D14di)
          {
              if (ReferenceEquals(_rootM03D14di._singletonM03D14di34_Dependency, null))
              {
                  _singletonM03D14di34_Dependency = new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency();
                  _rootM03D14di._singletonM03D14di34_Dependency = _singletonM03D14di34_Dependency;
              }
          }
      }
      return _rootM03D14di._singletonM03D14di34_Dependency;
    }
  }
  
  /// <summary>
  /// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Service"/>.
  /// </summary>
  public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService RootM03D14di0002
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (ReferenceEquals(_rootM03D14di._singletonM03D14di34_Dependency, null))
      {
          lock (_lockM03D14di)
          {
              if (ReferenceEquals(_rootM03D14di._singletonM03D14di34_Dependency, null))
              {
                  _singletonM03D14di34_Dependency = new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency();
                  _rootM03D14di._singletonM03D14di34_Dependency = _singletonM03D14di34_Dependency;
              }
          }
      }
      return new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Service(_rootM03D14di._singletonM03D14di34_Dependency);
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
    return ResolverM03D14di<T>.Value.Resolve(this);
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
    return ResolverM03D14di<T>.Value.ResolveByTag(this, tag);
  }
  
  /// <summary>
  /// Resolves the composition root.
  /// </summary>
  /// <param name="type">The type of the composition root.</param>
  /// <returns>A composition root.</returns>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object GetService(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D14di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D14di;
    do {
      ref var pair = ref _bucketsM03D14di[index];
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
  public object GetRequiredKeyedService(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D14di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D14di;
    do {
      ref var pair = ref _bucketsM03D14di[index];
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
          "    -IDependency RootM03D14di0001\n" +
          "    -IService RootM03D14di0002\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object GetService(Type type)\n" +
          "    + object GetRequiredKeyedService(Type type, object? tag)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \"Dependency Key\" \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \"Service Key\" \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"Singleton\" Dependency : \"Dependency Key\"  IDependency\n" +
        "  Composition ..> Dependency : IDependency RootM03D14di0001<br/>provides \"Dependency Key\" IDependency\n" +
        "  Composition ..> Service : IService RootM03D14di0002<br/>provides \"Service Key\" IService";
  }
  
  private readonly static int _bucketSizeM03D14di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D14di;
  
  static Composition()
  {
    var valResolverM03D14di_0000 = new ResolverM03D14di_0000();
    ResolverM03D14di<Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency>.Value = valResolverM03D14di_0000;
    var valResolverM03D14di_0001 = new ResolverM03D14di_0001();
    ResolverM03D14di<Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService>.Value = valResolverM03D14di_0001;
    _bucketsM03D14di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM03D14di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency), valResolverM03D14di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService), valResolverM03D14di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM03D14di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D14di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D14di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency>
  {
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency.");
    }
    
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case "Dependency Key":
          return composition.RootM03D14di0001;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency.");
    }
  }
  
  private sealed class ResolverM03D14di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService>
  {
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService.");
    }
    
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case "Service Key":
          return composition.RootM03D14di0002;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService.");
    }
  }
  #endregion
}
```

</blockquote></details>

