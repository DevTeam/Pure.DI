#### Generic with constraints composition roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/GenericsWithConstraintsCompositionRootsScenario.cs)

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
    +IServiceᐸT49ˏT51ᐳ GetMyRootᐸT49ˏT51ᐳ()
    +IServiceᐸT49ˏT51ᐳ GetOtherServiceᐸT49ˏT51ᐳ()
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  ServiceᐸT49ˏT51ᐳ --|> IServiceᐸT49ˏT51ᐳ : 
  class ServiceᐸT49ˏT51ᐳ {
    +Service(IDependencyᐸT49ᐳ dependency)
  }
  OtherServiceᐸT49ˏT51ᐳ --|> IServiceᐸT49ˏT51ᐳ : "Other" 
  class OtherServiceᐸT49ˏT51ᐳ
  DependencyᐸT49ᐳ --|> IDependencyᐸT49ᐳ : 
  class DependencyᐸT49ᐳ {
    +Dependency()
  }
  class IServiceᐸT49ˏT51ᐳ {
    <<abstract>>
  }
  class IDependencyᐸT49ᐳ {
    <<abstract>>
  }
  Composition ..> ServiceᐸT49ˏT51ᐳ : IServiceᐸT49ˏT51ᐳ GetMyRootᐸT49ˏT51ᐳ()<br/>provides IServiceᐸT49ˏT51ᐳ
  Composition ..> OtherServiceᐸT49ˏT51ᐳ : IServiceᐸT49ˏT51ᐳ GetOtherServiceᐸT49ˏT51ᐳ()<br/>provides "Other" IServiceᐸT49ˏT51ᐳ
  ServiceᐸT49ˏT51ᐳ *--  DependencyᐸT49ᐳ : IDependencyᐸT49ᐳ
  OtherServiceᐸT49ˏT51ᐳ *--  DependencyᐸT49ᐳ : IDependencyᐸT49ᐳ
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
/// <see cref="Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.IService{T49, T51}"/> <see cref="GetMyRoot{T49, T51}()"/>
/// </term>
/// <description>
/// Specifies to create a regular public method<br/>
/// to get a composition root of type Service&lt;T&gt;<br/>
/// with the name "GetMyRoot"
/// </description>
/// </item>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.IService{T49, T51}"/> <see cref="GetOtherService{T49, T51}()"/>
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
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.Service{T49, T51}"/> using the composition root <see cref="GetMyRoot{T49, T51}()"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.GetMyRoot&lt;T49, T51&gt;();
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqlVUFOwzAQ_IrlMxKlBYX21iYF9YCQSo6-OMmqBJo4ckykqOob6F-48Ade0Z-Q2C1JwXZDe1k53tnZ2fXaWeGQRYBHOFzSPPdiuuA0IZyk8hu5LMlYHouYpYi89XrOpPbVq_5k9gS8iEPYbj796-HXu39ztd18oHsQD-WcMXG4L2NupR124XgUz8C1Xj0T8tEccrYsJPq_MBa8QCjq9eAOCbr4E6YA-1gV5JcZIFGZE9Ausqd0vHql745Eeo11XMkyRYZuSu8YNadqhrXPd4dS6mceZJBGkIalCqsjop89nXjzCZ5cAZacBLdqscxJhdGpNic3oQ-aZwK1W9dgdFOo2qPYLAU3hAOl0KVBLjjdDc10Z3WkxzV2p_z9BPQlStmqadYDO-OJkOoCTi5VnoyzIo7A1LAOQo-Mo1Vtx8fIpLmZW7N6y0UZtycWGQewrkDj63IVz8-AL3ACPKFxVP1LVgRXCRMgeERwRPkrwWu8_gayG20n">Class diagram</a><br/>
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
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.Service{T49, T51}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetMyRoot&lt;T49, T51&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.IService<T49, T51> GetMyRoot<T49, T51>()
    where T49: System.IDisposable
    where T51: struct
  {
    return new Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.Service<T49, T51>(new Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.Dependency<T49>());
  }
  
  /// <summary>
  /// Specifies to create a regular public method<br/>
  /// to get a composition root of type OtherService&lt;T&gt;<br/>
  /// with the name "GetOtherService"<br/>
  /// using the "Other" tag
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.OtherService{T49, T51}"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetOtherService&lt;T49, T51&gt;();
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.IService<T49, T51> GetOtherService<T49, T51>()
    where T49: System.IDisposable
    where T51: struct
  {
    Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.OtherService<T49, T51> transientM03D14di0_OtherService;
    {
        var dependency_M03D14di1 = new Pure.DI.UsageTests.Basics.GenericsWithConstraintsCompositionRootsScenario.Dependency<T49>();
        transientM03D14di0_OtherService = new OtherService<T49, T51>(dependency_M03D14di1);
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
          "    +IServiceᐸT49ˏT51ᐳ GetMyRootᐸT49ˏT51ᐳ()\n" +
          "    +IServiceᐸT49ˏT51ᐳ GetOtherServiceᐸT49ˏT51ᐳ()\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  ServiceᐸT49ˏT51ᐳ --|> IServiceᐸT49ˏT51ᐳ : \n" +
        "  class ServiceᐸT49ˏT51ᐳ {\n" +
          "    +Service(IDependencyᐸT49ᐳ dependency)\n" +
        "  }\n" +
        "  OtherServiceᐸT49ˏT51ᐳ --|> IServiceᐸT49ˏT51ᐳ : \"Other\" \n" +
        "  class OtherServiceᐸT49ˏT51ᐳ\n" +
        "  DependencyᐸT49ᐳ --|> IDependencyᐸT49ᐳ : \n" +
        "  class DependencyᐸT49ᐳ {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IServiceᐸT49ˏT51ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependencyᐸT49ᐳ {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Composition ..> ServiceᐸT49ˏT51ᐳ : IServiceᐸT49ˏT51ᐳ GetMyRootᐸT49ˏT51ᐳ()<br/>provides IServiceᐸT49ˏT51ᐳ\n" +
        "  Composition ..> OtherServiceᐸT49ˏT51ᐳ : IServiceᐸT49ˏT51ᐳ GetOtherServiceᐸT49ˏT51ᐳ()<br/>provides \"Other\" IServiceᐸT49ˏT51ᐳ\n" +
        "  ServiceᐸT49ˏT51ᐳ *--  DependencyᐸT49ᐳ : IDependencyᐸT49ᐳ\n" +
        "  OtherServiceᐸT49ˏT51ᐳ *--  DependencyᐸT49ᐳ : IDependencyᐸT49ᐳ";
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

