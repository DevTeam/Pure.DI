#### Tracking disposable instances

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/TrackingDisposableInstancesScenario.cs)

```c#
interface IDependency
{
    bool IsDisposed { get; }
}

class TransientDependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    public event Action<IDisposable> OnNewDisposable;

    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to call a partial method
            // named OnNewInstance when an instance is created
            .Hint(Hint.OnNewInstance, "On")

            // Specifies to call the partial method
            // only for instances with lifetime
            // Transient, PerResolve and PerBlock
            .Hint(
                Hint.OnNewInstanceLifetimeRegularExpression,
                "Transient|PerResolve|PerBlock")

            .Bind<IDependency>().To<TransientDependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime)
    {
        if (value is IDisposable disposable
            && OnNewDisposable is {} onNewDisposable)
        {
            onNewDisposable(disposable);
        }
    }
}

var composition = new Composition();
        
// Tracking disposable instances within a composition
var disposables = new Stack<IDisposable>();
composition.OnNewDisposable += disposable =>
    disposables.Push(disposable);
        
var service = composition.Root;
disposables.Count.ShouldBe(1);
        
// Disposal of instances in reverse order
while (disposables.TryPop(out var disposable))
{
    disposable.Dispose();
}
        
// Verifies that the disposable instance
// has been disposed of
service.Dependency.IsDisposed.ShouldBeTrue();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService Root
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  TransientDependency --|> IDependency : 
  class TransientDependency {
    +TransientDependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Service *--  TransientDependency : IDependency
  Composition ..> Service : IService Root
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
/// <see cref="Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.Service"/> Root
/// </term>
/// <description>
/// </description>
/// </item>
/// </list>
/// </para>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.Service"/> using the composition root <see cref="Root"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.Root;
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqdU0FuwjAQ_MrK5x5QckjLDRIqcaU5-mKSFXVL7Mh2kRDiD_yll36HnzS2g-KEpK16Wa3Xs7OzY_lEClkimZNiz7TOONspVlFFhTtDKqtaam64FEA_ZrNkae9sFi3XL6gOvEDYSGm6MuSwQS33B7xevvLr5dOVH118-hUmt29YGJvHz2DY7q7NA269vik_1gimCf9Ap_DzyCSzWa6Y0ByFybBGUaIojs6PKOtikjqOFawHoHgBnaNTTKGzI5gxE7202ytMywkRPS3hRTi_rfuZ4TZlX89AiScdbt8Rx15RyrbaKNZavmrjGNO4vr_T9KxZhAbB5Ds0BgUbWJbhH4jcLB8bewfu3v0K8kAqVBXjZfPLTpSYV6yQkjklJVPvlJzJ-RtSlDKY">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// </summary>
/// <seealso cref="Pure.DI.DI.Setup"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D01di;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D01di = this;
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D01di = baseComposition._rootM03D01di;
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService Root
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      var transientM03D01di1_TransientDependency = new Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.TransientDependency();
      OnNewInstance<Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.TransientDependency>(ref transientM03D01di1_TransientDependency, null, Pure.DI.Lifetime.Transient);
      var transientM03D01di0_Service = new Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.Service(transientM03D01di1_TransientDependency);
      OnNewInstance<Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.Service>(ref transientM03D01di0_Service, null, Pure.DI.Lifetime.Transient);
      return transientM03D01di0_Service;
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
    return ResolverM03D01di<T>.Value.Resolve(this);
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
    return ResolverM03D01di<T>.Value.ResolveByTag(this, tag);
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
    var index = (int)(_bucketSizeM03D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D01di;
    do {
      ref var pair = ref _bucketsM03D01di[index];
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
    var index = (int)(_bucketSizeM03D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    var finish = index + _bucketSizeM03D01di;
    do {
      ref var pair = ref _bucketsM03D01di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  partial void OnNewInstance<T>(ref T value, object? tag, global::Pure.DI.Lifetime lifetime);
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
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  TransientDependency --|> IDependency : \n" +
        "  class TransientDependency {\n" +
          "    +TransientDependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service *--  TransientDependency : IDependency\n" +
        "  Composition ..> Service : IService Root";
  }
  
  private readonly static int _bucketSizeM03D01di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D01di;
  
  static Composition()
  {
    var valResolverM03D01di_0000 = new ResolverM03D01di_0000();
    ResolverM03D01di<Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService>.Value = valResolverM03D01di_0000;
    _bucketsM03D01di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSizeM03D01di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService), valResolverM03D01di_0000)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM03D01di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D01di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D01di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService>
  {
    public Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    public Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Root;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Basics.TrackingDisposableInstancesScenario.IService.");
    }
  }
  #endregion
}
```

</blockquote></details>

