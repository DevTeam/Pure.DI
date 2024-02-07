#### Scope

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/ScopeScenario.cs)

A _scope_ scenario can be easily implemented with singleton instances and child composition:

```c#

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency: IDependency, IDisposable
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
    public IDependency Dependency => dependency;
}

interface ISession : IDisposable
{
    IService SessionRoot { get; }
}

class Session(Composition composition):
    Composition(composition),
    ISession;

class Program(Func<ISession> sessionFactory)
{
    public ISession CreateSession() => sessionFactory();
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // This singleton will be actually created
            // in the scope of a session
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Bind<ISession>().To<Session>()
            .Root<IService>("SessionRoot")
            .Root<Program>("ProgramRoot");
}

var composition = new Composition();
var programRoot = composition.ProgramRoot;
        
// Creates session #1
var session1 = programRoot.CreateSession();
var dependencyInSession1 = session1.SessionRoot.Dependency;
dependencyInSession1.ShouldBe(session1.SessionRoot.Dependency);
        
// Creates session #2
using var session2 = programRoot.CreateSession();
var dependencyInSession2 = session2.SessionRoot.Dependency;
dependencyInSession1.ShouldNotBe(dependencyInSession2);
        
// Disposes of session #1
session1.Dispose();
dependencyInSession1.IsDisposed.ShouldBeTrue();
        
// Session #2 is still not finalized
session2.SessionRoot.Dependency.IsDisposed.ShouldBeFalse();
        
// Disposes of session #1
session2.Dispose();
dependencyInSession2.IsDisposed.ShouldBeTrue();
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
  class Program {
    +Program(FuncᐸISessionᐳ sessionFactory)
  }
  class Composition
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  Session --|> ISession : 
  class Session {
    +Session(Composition composition)
  }
  class FuncᐸISessionᐳ
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  class ISession {
    <<abstract>>
  }
  Program o--  "PerResolve" FuncᐸISessionᐳ : FuncᐸISessionᐳ
  Service o--  "Singleton" Dependency : IDependency
  Session *--  Composition : Composition
  Composition ..> Service : IService SessionRoot
  Composition ..> Program : Program ProgramRoot
  FuncᐸISessionᐳ *--  Session : ISession
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition: global::System.IDisposable
{
  private readonly global::System.IDisposable[] _disposableSingletonsM02D07di;
  private int _disposeIndexM02D07di;
  private Pure.DI.UsageTests.Lifetimes.ScopeScenario.Dependency _singletonM02D07di34_Dependency;
  
  public Composition()
  {
    _disposableSingletonsM02D07di = new global::System.IDisposable[1];
  }
  
  internal Composition(Composition parent)
  {
    lock (parent._disposableSingletonsM02D07di)
    {
      _disposableSingletonsM02D07di = new global::System.IDisposable[1 - parent._disposeIndexM02D07di];
      _singletonM02D07di34_Dependency = parent._singletonM02D07di34_Dependency;
    }
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService SessionRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (object.ReferenceEquals(_singletonM02D07di34_Dependency, null))
      {
          lock (_disposableSingletonsM02D07di)
          {
              if (object.ReferenceEquals(_singletonM02D07di34_Dependency, null))
              {
                  _singletonM02D07di34_Dependency = new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Dependency();
                  _disposableSingletonsM02D07di[_disposeIndexM02D07di++] = _singletonM02D07di34_Dependency;
              }
          }
      }
      return new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Service(_singletonM02D07di34_Dependency);
    }
  }
  
  public Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program ProgramRoot
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      var perResolveM02D07di39_Func = default(System.Func<Pure.DI.UsageTests.Lifetimes.ScopeScenario.ISession>);
      perResolveM02D07di39_Func = new global::System.Func<Pure.DI.UsageTests.Lifetimes.ScopeScenario.ISession>(
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
      () =>
      {
          var transientM02D07di2_Composition = this;
          var factory_M02D07di1 = new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Session(transientM02D07di2_Composition);
          return factory_M02D07di1;
      });
      return new Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program(perResolveM02D07di39_Func);
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM02D07di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM02D07di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM02D07di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D07di;
    do {
      ref var pair = ref _bucketsM02D07di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM02D07di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D07di;
    do {
      ref var pair = ref _bucketsM02D07di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  #endregion
  
  public void Dispose()
  {
    lock (_disposableSingletonsM02D07di)
    {
      while (_disposeIndexM02D07di > 0)
      {
        var disposableInstance = _disposableSingletonsM02D07di[--_disposeIndexM02D07di];
        try
        {
          disposableInstance.Dispose();
        }
        catch(Exception exception)
        {
          OnDisposeException(disposableInstance, exception);
        }
      }
      
      _singletonM02D07di34_Dependency = null;
    }
  }
  
  partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : global::System.IDisposable;
  
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
        "  class Program {\n" +
          "    +Program(FuncᐸISessionᐳ sessionFactory)\n" +
        "  }\n" +
        "  class Composition\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  Session --|> ISession : \n" +
        "  class Session {\n" +
          "    +Session(Composition composition)\n" +
        "  }\n" +
        "  class FuncᐸISessionᐳ\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class ISession {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Program o--  \"PerResolve\" FuncᐸISessionᐳ : FuncᐸISessionᐳ\n" +
        "  Service o--  \"Singleton\" Dependency : IDependency\n" +
        "  Session *--  Composition : Composition\n" +
        "  Composition ..> Service : IService SessionRoot\n" +
        "  Composition ..> Program : Program ProgramRoot\n" +
        "  FuncᐸISessionᐳ *--  Session : ISession";
  }
  
  private readonly static int _bucketSizeM02D07di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM02D07di;
  
  static Composition()
  {
    var valResolverM02D07di_0000 = new ResolverM02D07di_0000();
    ResolverM02D07di<Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService>.Value = valResolverM02D07di_0000;
    var valResolverM02D07di_0001 = new ResolverM02D07di_0001();
    ResolverM02D07di<Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program>.Value = valResolverM02D07di_0001;
    _bucketsM02D07di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM02D07di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService), valResolverM02D07di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program), valResolverM02D07di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM02D07di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM02D07di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM02D07di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.ScopeScenario.IService>
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
  
  private sealed class ResolverM02D07di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.ScopeScenario.Program>
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

