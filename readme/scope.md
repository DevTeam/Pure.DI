#### Scope

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/ScopeScenario.cs)

The _Scoped_ lifetime ensures that there will be a single instance of the dependency for each scope.

```c#
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition composition) : Composition(composition);

class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().As(Scoped).To<Dependency>()
            .Bind<IService>().To<Service>()
            // Session composition root
            .Root<IService>("SessionRoot")
            // Program composition root
            .Root<Program>("ProgramRoot");
}

var composition = new Composition();
var program = composition.ProgramRoot;
        
// Creates session #1
var session1 = program.CreateSession();
var dependency1 = session1.SessionRoot.Dependency;
var dependency12 = session1.SessionRoot.Dependency;
        
// Checks the identity of scoped instances in the same session
dependency1.ShouldBe(dependency12);
        
// Creates session #2
var session2 = program.CreateSession();
var dependency2 = session2.SessionRoot.Dependency;
        
// Checks that the scoped instances are not identical in different sessions
dependency1.ShouldNotBe(dependency2);
        
// Disposes of session #1
session1.Dispose();
// Checks that the scoped instance is finalized
dependency1.IsDisposed.ShouldBeTrue();
        
// Disposes of session #2
session2.Dispose();
// Checks that the scoped instance is finalized
dependency2.IsDisposed.ShouldBeTrue();
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +Program ProgramRoot
    +IService SessionRoot
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object Resolve(Type type)
    + object Resolve(Type type, object? tag)
  }
  Composition --|> IDisposable
  class Composition
  class Session {
    +Session(Composition composition)
  }
  class Program {
    +Program(FuncᐸSessionᐳ sessionFactory)
  }
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  class FuncᐸSessionᐳ
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Session *--  Composition : Composition
  Program o--  "PerResolve" FuncᐸSessionᐳ : FuncᐸSessionᐳ
  Service o--  "Scoped" Dependency : IDependency
  Composition ..> Service : IService SessionRoot<br/>provides IService
  Composition ..> Program : Program ProgramRoot<br/>provides Program
  FuncᐸSessionᐳ *--  Session : Session
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
/// <see cref="Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program"/> <see cref="ProgramRoot"/><br/>or using <see cref="Resolve{T}()"/> method: <c>Resolve&lt;Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program&gt;()</c>
/// </term>
/// <description>
/// Program composition root
/// </description>
/// </item>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService"/> <see cref="SessionRoot"/><br/>or using <see cref="Resolve{T}()"/> method: <c>Resolve&lt;Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService&gt;()</c>
/// </term>
/// <description>
/// Session composition root
/// </description>
/// </item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program"/> using the composition root <see cref="ProgramRoot"/>:
/// <code>
/// using var composition = new Composition();
/// var instance = composition.ProgramRoot;
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqdVEtuwjAQvYrldaUiWNCyg6RI7BCw9MZxRjSUxJFtkBDiDtylm16Hm9SJY9lJDK3YTBzP543ffM6Y8RTwBLM9lTLO6FbQnAhS1P8o4nnJZaYyXiByGAzGs0pXnYazpeCVMWq-K86VUy7WII4ZA7QGKbV3W4s2aAWS749wu_5sbtfv-vqtlu9_mvFkB0xV59EcKbrtuRkD62ucNqcSkNLiCesIPYYcx9Wpy9UwdnIc1b4faBFnUlvRZA9Blt1lQ1yP9ube5OljMi9KID0T1RbtTjFN1PmhYJrxBknzjqQ5zilTXJxC4WMooUihYKdHj28bjabIZdbR-ck5VahPDL7tt_vgvkUL2Ve0ma7vDaafe9rOJ0h0j0On6tLgMEcm2YgmUgnNtUm9kQGQ1pueCeM32XDqc4d6s6856zSqbSbediR4CcKOE-4zYaMFKbIv6sVcM15CquP1m8gjNDiI9dON1I3Q6YPgpqrpSwR5NT6l4McsBUf4P2D8SdMwoW15D8UOowYJk9ctlV9GjWVXhPbHLzgHkdMs1Tv-TLD6hFwXZUJwSsUXwRd8-QWsmxpE">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// <seealso cref="Pure.DI.DI.Setup"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind(object[])"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind{T}(object[])"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition: global::System.IDisposable
{
  private readonly Composition _rootM03D14di;
  private readonly object _lockM03D14di;
  private readonly global::System.IDisposable[] _disposablesM03D14di;
  private int _disposeIndexM03D14di;
  private Pure.DI.UsageTests.Lifetimes.ScopeScenario.Dependency _scopedM03D14di34_Dependency;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D14di = this;
    _lockM03D14di = new object();
    _disposablesM03D14di = new global::System.IDisposable[1];
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D14di = baseComposition._rootM03D14di;
    _lockM03D14di = _rootM03D14di._lockM03D14di;
    _disposablesM03D14di = new global::System.IDisposable[1];
  }
  
  #region Composition Roots
  /// <summary>
  /// Session composition root
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Lifetimes.ScopeScenario.Service"/>:
  /// <code>
  /// using var composition = new Composition();
  /// var instance = composition.SessionRoot;
  /// </code>
  /// </example>
  public Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService SessionRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (ReferenceEquals(_scopedM03D14di34_Dependency, null))
      {
          lock (_lockM03D14di)
          {
              if (ReferenceEquals(_scopedM03D14di34_Dependency, null))
              {
                  _scopedM03D14di34_Dependency = new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Dependency();
                  _disposablesM03D14di[_disposeIndexM03D14di++] = _scopedM03D14di34_Dependency;
              }
          }
      }
      return new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Service(_scopedM03D14di34_Dependency);
    }
  }
  
  /// <summary>
  /// Program composition root
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program"/>:
  /// <code>
  /// using var composition = new Composition();
  /// var instance = composition.ProgramRoot;
  /// </code>
  /// </example>
  public Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program ProgramRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      var perResolveM03D14di40_Func = default(System.Func<Pure.DI.UsageTests.Lifetimes.ScopeScenario.Session>);
      perResolveM03D14di40_Func = new global::System.Func<Pure.DI.UsageTests.Lifetimes.ScopeScenario.Session>(
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
      () =>
      {
          var transientM03D14di2_Composition = this;
          var factory_M03D14di1 = new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Session(transientM03D14di2_Composition);
          return factory_M03D14di1;
      });
      return new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program(perResolveM03D14di40_Func);
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
  public object Resolve(global::System.Type type)
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
  public object Resolve(global::System.Type type, object? tag)
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
  /// <inheritdoc/>
  /// </summary>
  public void Dispose()
  {
    lock (_lockM03D14di)
    {
      while (_disposeIndexM03D14di > 0)
      {
        var disposableInstance = _disposablesM03D14di[--_disposeIndexM03D14di];
        try
        {
          disposableInstance.Dispose();
        }
        catch(Exception exception)
        {
          OnDisposeException(disposableInstance, exception);
        }
      }
      
      _scopedM03D14di34_Dependency = null;
    }
  }
  
  partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : global::System.IDisposable;
  
  /// <summary>
  /// This method provides a class diagram in mermaid format. To see this diagram, simply call the method and copy the text to this site https://mermaid.live/.
  /// </summary>
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +Program ProgramRoot\n" +
          "    +IService SessionRoot\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  Composition --|> IDisposable\n" +
        "  class Composition\n" +
        "  class Session {\n" +
          "    +Session(Composition composition)\n" +
        "  }\n" +
        "  class Program {\n" +
          "    +Program(FuncᐸSessionᐳ sessionFactory)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class FuncᐸSessionᐳ\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Session *--  Composition : Composition\n" +
        "  Program o--  \"PerResolve\" FuncᐸSessionᐳ : FuncᐸSessionᐳ\n" +
        "  Service o--  \"Scoped\" Dependency : IDependency\n" +
        "  Composition ..> Service : IService SessionRoot<br/>provides IService\n" +
        "  Composition ..> Program : Program ProgramRoot<br/>provides Program\n" +
        "  FuncᐸSessionᐳ *--  Session : Session";
  }
  
  private readonly static int _bucketSizeM03D14di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D14di;
  
  static Composition()
  {
    var valResolverM03D14di_0000 = new ResolverM03D14di_0000();
    ResolverM03D14di<Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService>.Value = valResolverM03D14di_0000;
    var valResolverM03D14di_0001 = new ResolverM03D14di_0001();
    ResolverM03D14di<Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program>.Value = valResolverM03D14di_0001;
    _bucketsM03D14di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM03D14di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService), valResolverM03D14di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program), valResolverM03D14di_0001)
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
  
  private sealed class ResolverM03D14di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService>
  {
    public Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService Resolve(Composition composition)
    {
      return composition.SessionRoot;
    }
    
    public Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.SessionRoot;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService.");
    }
  }
  
  private sealed class ResolverM03D14di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program>
  {
    public Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program Resolve(Composition composition)
    {
      return composition.ProgramRoot;
    }
    
    public Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.ProgramRoot;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program.");
    }
  }
  #endregion
}
```

</blockquote></details>

