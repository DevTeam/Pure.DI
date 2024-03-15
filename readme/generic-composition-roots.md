#### Generic composition roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericsCompositionRootsScenario.cs)

A generic composition root is represented by a method.

```c#
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T>
    // with the name "GetMyRoot"
    .Root<IService<TT>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TT>>("GetOtherService", "Other");

var composition = new Composition();
        
// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyRoot<int>();
        
// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetOtherService<string>();
```

When a generic composition root is used, `Resolve` methods cannot be used to resolve them.

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IServiceᐸT66ᐳ GetMyRootᐸT66ᐳ()
    +IServiceᐸT66ᐳ GetOtherServiceᐸT66ᐳ()
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  ServiceᐸT66ᐳ --|> IServiceᐸT66ᐳ : 
  class ServiceᐸT66ᐳ {
    +Service(IDependencyᐸT66ᐳ dependency)
  }
  OtherServiceᐸT66ᐳ --|> IServiceᐸT66ᐳ : "Other" 
  class OtherServiceᐸT66ᐳ
  DependencyᐸT66ᐳ --|> IDependencyᐸT66ᐳ : 
  class DependencyᐸT66ᐳ {
    +Dependency()
  }
  class IServiceᐸT66ᐳ {
    <<abstract>>
  }
  class IDependencyᐸT66ᐳ {
    <<abstract>>
  }
  Composition ..> ServiceᐸT66ᐳ : IServiceᐸT66ᐳ GetMyRootᐸT66ᐳ()
  Composition ..> OtherServiceᐸT66ᐳ : IServiceᐸT66ᐳ GetOtherServiceᐸT66ᐳ()
  ServiceᐸT66ᐳ *--  DependencyᐸT66ᐳ : IDependencyᐸT66ᐳ
  OtherServiceᐸT66ᐳ *--  DependencyᐸT66ᐳ : IDependencyᐸT66ᐳ
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
/// <see cref="Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.IService{T66}"/> <see cref="GetMyRoot{T66}()"/>
/// </term>
/// <description>
/// Specifies to create a regular public method<br/>
/// to get a composition root of type Service&lt;T&gt;<br/>
/// with the name "GetMyRoot"
/// </description>
/// </item>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.IService{T66}"/> <see cref="GetOtherService{T66}()"/>
/// </term>
/// <description>
/// Specifies to create a regular public method<br/>
/// to get a composition root of type OtherService&lt;T&gt;<br/>
/// with the name "GetOtherService"<br/>
/// using the "Other" tag
/// </description>
/// </item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.Service{T66}"/> using the composition root <see cref="GetMyRoot{T66}()"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.GetMyRoot&lt;T66&gt;();
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqlVMFuwjAM_ZXI5x0QSHTjBi1MHKZJrMdcQmuxbrSp0gypQvwD_7LLfoc_WZt0tKPJysrFcuPn51fbyR4CHiJMINiyLPMithEspoIm6pu4PE55FsmIJ4R-DAbOrIyV3nC2fEGxiwI8Hb_88fh0_CSPKJ_yFefyfKSQ98o-dGQ-y1cUlwFzPvHJCjO-3Sngf2F8_YaBLP3Rgki2aaVpwE-uTvLzFIksTA-0S_4u6Xil1-qJAnm1dVxFMCft9qnAlNRzMyKaw6sAWu7SwxSTEJMgrzPC85lJrXFafSSDYqLQEG_ehCJskmkvaUP_apQN1OxVjTHtme6HZjP_Zs010uJcts6kYNVGzCtr4uuWdz3l5V0eKpS2Rb9sE-pzzTtK2ZfHVu-qx8G8jtPmhhDrwMvKhljHrt9ODncQo4hZFBaP8J5CUStGChMKIRPvFA5w-AYndB2D">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// <seealso cref="Pure.DI.DI.Setup"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind(object[])"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind{T}(object[])"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D15di;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D15di = this;
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D15di = baseComposition._rootM03D15di;
  }
  
  #region Composition Roots
  /// <summary>
  /// Specifies to create a regular public method<br/>
  /// to get a composition root of type Service&lt;T&gt;<br/>
  /// with the name "GetMyRoot"
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.Service{T66}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetMyRoot&lt;T66&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.IService<T66> GetMyRoot<T66>()
  {
    return new Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.Service<T66>(new Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.Dependency<T66>());
  }
  
  /// <summary>
  /// Specifies to create a regular public method<br/>
  /// to get a composition root of type OtherService&lt;T&gt;<br/>
  /// with the name "GetOtherService"<br/>
  /// using the "Other" tag
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.OtherService{T66}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetOtherService&lt;T66&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.IService<T66> GetOtherService<T66>()
  {
    Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.OtherService<T66> transientM03D15di0_OtherService;
    {
        var dependency_M03D15di1 = new Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario.Dependency<T66>();
        transientM03D15di0_OtherService = new OtherService<T66>(dependency_M03D15di1);
    }
    return transientM03D15di0_OtherService;
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
    return ResolverM03D15di<T>.Value.Resolve(this);
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
    return ResolverM03D15di<T>.Value.ResolveByTag(this, tag);
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
          "    +IServiceᐸT66ᐳ GetMyRootᐸT66ᐳ()\n" +
          "    +IServiceᐸT66ᐳ GetOtherServiceᐸT66ᐳ()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  ServiceᐸT66ᐳ --|> IServiceᐸT66ᐳ : \n" +
        "  class ServiceᐸT66ᐳ {\n" +
          "    +Service(IDependencyᐸT66ᐳ dependency)\n" +
        "  }\n" +
        "  OtherServiceᐸT66ᐳ --|> IServiceᐸT66ᐳ : \"Other\" \n" +
        "  class OtherServiceᐸT66ᐳ\n" +
        "  DependencyᐸT66ᐳ --|> IDependencyᐸT66ᐳ : \n" +
        "  class DependencyᐸT66ᐳ {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IServiceᐸT66ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸT66ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Composition ..> ServiceᐸT66ᐳ : IServiceᐸT66ᐳ GetMyRootᐸT66ᐳ()\n" +
        "  Composition ..> OtherServiceᐸT66ᐳ : IServiceᐸT66ᐳ GetOtherServiceᐸT66ᐳ()\n" +
        "  ServiceᐸT66ᐳ *--  DependencyᐸT66ᐳ : IDependencyᐸT66ᐳ\n" +
        "  OtherServiceᐸT66ᐳ *--  DependencyᐸT66ᐳ : IDependencyᐸT66ᐳ";
  }
  
  
  #region Resolvers
  private sealed class ResolverM03D15di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D15di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  #endregion
}
```

</blockquote></details>

