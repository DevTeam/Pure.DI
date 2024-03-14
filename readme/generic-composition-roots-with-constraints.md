#### Generic composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/GenericCompositionRootsWithConstraintsScenario.cs)

```c#
interface IDependency<T>
    where T: IDisposable;

class Dependency<T> : IDependency<T>
    where T: IDisposable;

interface IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class OtherService<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TTDisposable>>()
    .Bind().To<Service<TTDisposable, TTS>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TTDisposable> dependency);
        return new OtherService<TTDisposable, TTS>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T>
    // with the name "GetMyRoot"
    .Root<IService<TTDisposable, TTS>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TTDisposable, TTS>>("GetOtherService", "Other");

var composition = new Composition();
        
// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyRoot<Stream, double>();
        
// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetOtherService<Stream, DateTime>();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IServiceᐸTˏT2ᐳ GetMyRootᐸTˏT2ᐳ()
    +IServiceᐸTˏT2ᐳ GetOtherServiceᐸTˏT2ᐳ()
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  ServiceᐸTˏT2ᐳ --|> IServiceᐸTˏT2ᐳ : 
  class ServiceᐸTˏT2ᐳ {
    +Service(IDependencyᐸTᐳ dependency)
  }
  OtherServiceᐸTˏT2ᐳ --|> IServiceᐸTˏT2ᐳ : "Other" 
  class OtherServiceᐸTˏT2ᐳ
  DependencyᐸTᐳ --|> IDependencyᐸTᐳ : 
  class DependencyᐸTᐳ {
    +Dependency()
  }
  class IServiceᐸTˏT2ᐳ {
    <<abstract>>
  }
  class IDependencyᐸTᐳ {
    <<abstract>>
  }
  Composition ..> ServiceᐸTˏT2ᐳ : IServiceᐸTˏT2ᐳ GetMyRootᐸTˏT2ᐳ()<br/>provides IServiceᐸTˏT2ᐳ
  Composition ..> OtherServiceᐸTˏT2ᐳ : IServiceᐸTˏT2ᐳ GetOtherServiceᐸTˏT2ᐳ()<br/>provides "Other" IServiceᐸTˏT2ᐳ
  ServiceᐸTˏT2ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
  OtherServiceᐸTˏT2ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
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
/// <see cref="Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.IService{T, T2}"/> <see cref="GetMyRoot{T, T2}()"/>
/// </term>
/// <description>
/// Specifies to create a regular public method<br/>
/// to get a composition root of type Service&lt;T&gt;<br/>
/// with the name "GetMyRoot"
/// </description>
/// </item>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.IService{T, T2}"/> <see cref="GetOtherService{T, T2}()"/>
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
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.Service{T, T2}"/> using the composition root <see cref="GetMyRoot{T, T2}()"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.GetMyRoot&lt;T, T2&gt;();
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqlVEFOwzAQ_IrlMxJVegj01iYF9YCQSo6-OMmqGJo4ckykqOob6F-48Ade0Z-Q2IEEYjclXFaOd3Y82R17hyMeA57haEvz3Gd0I2hCBEnVN_J4kvGcScZTRF4mE3dR5-qVs1g9gChYBMfDe_DxGjjHwxu6BXlXrjmXnU2FvlLxerD6Xj6C6KfMHChAa8j5tlDQv8J4-ASRrNfTGyTpplemAV-1uigoM0CyCiPQHjp9pOvXK0NfFMxvo-spiiUyNVGl5qidoQXTHWUD0aJXPmSQxpBGZdMvFH_vmBRbZjZONlZsBHd-wOaJCtCXaj_UjP3RKjOk26kWYfKa7ofmsv1kyzbV0jwa5lLQxhfLJpoYh-SdT_j7XjsKpWPVK_t8Rl16JSoU5FLTZ4IXLAZji84Qd8pudoXnPCw2na0pLYpt5p933Ygs9qo19zKD9-p_1PgCJyASyuLq6d8RXJ2UAMEzgmMqngne4_0nnqRcHQ">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// <seealso cref="Pure.DI.DI.Setup"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind(object[])"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind{T}(object[])"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D14di;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D14di = this;
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D14di = baseComposition._rootM03D14di;
  }
  
  #region Composition Roots
  /// <summary>
  /// Specifies to create a regular public method<br/>
  /// to get a composition root of type Service&lt;T&gt;<br/>
  /// with the name "GetMyRoot"
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.Service{T, T2}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetMyRoot&lt;T, T2&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.IService<T, T2> GetMyRoot<T, T2>()
    where T: System.IDisposable
    where T2: struct
  {
    return new Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.Service<T, T2>(new Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.Dependency<T>());
  }
  
  /// <summary>
  /// Specifies to create a regular public method<br/>
  /// to get a composition root of type OtherService&lt;T&gt;<br/>
  /// with the name "GetOtherService"<br/>
  /// using the "Other" tag
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.OtherService{T, T2}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetOtherService&lt;T, T2&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.IService<T, T2> GetOtherService<T, T2>()
    where T: System.IDisposable
    where T2: struct
  {
    Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.OtherService<T, T2> transientM03D14di0_OtherService;
    {
        var dependency_M03D14di1 = new Pure.DI.UsageTests.Basics.GenericCompositionRootsWithConstraintsScenario.Dependency<T>();
        transientM03D14di0_OtherService = new OtherService<T, T2>(dependency_M03D14di1);
    }
    return transientM03D14di0_OtherService;
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
          "    +IServiceᐸTˏT2ᐳ GetMyRootᐸTˏT2ᐳ()\n" +
          "    +IServiceᐸTˏT2ᐳ GetOtherServiceᐸTˏT2ᐳ()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  ServiceᐸTˏT2ᐳ --|> IServiceᐸTˏT2ᐳ : \n" +
        "  class ServiceᐸTˏT2ᐳ {\n" +
          "    +Service(IDependencyᐸTᐳ dependency)\n" +
        "  }\n" +
        "  OtherServiceᐸTˏT2ᐳ --|> IServiceᐸTˏT2ᐳ : \"Other\" \n" +
        "  class OtherServiceᐸTˏT2ᐳ\n" +
        "  DependencyᐸTᐳ --|> IDependencyᐸTᐳ : \n" +
        "  class DependencyᐸTᐳ {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IServiceᐸTˏT2ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸTᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Composition ..> ServiceᐸTˏT2ᐳ : IServiceᐸTˏT2ᐳ GetMyRootᐸTˏT2ᐳ()<br/>provides IServiceᐸTˏT2ᐳ\n" +
        "  Composition ..> OtherServiceᐸTˏT2ᐳ : IServiceᐸTˏT2ᐳ GetOtherServiceᐸTˏT2ᐳ()<br/>provides \"Other\" IServiceᐸTˏT2ᐳ\n" +
        "  ServiceᐸTˏT2ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ\n" +
        "  OtherServiceᐸTˏT2ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ";
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
  #endregion
}
```

</blockquote></details>

