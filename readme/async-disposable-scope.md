#### Async disposable scope

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/AsyncDisposableScopeScenario.cs)

```c#
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
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
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().As(Scoped).To<Dependency>()

            // Session composition root
            .RootBind<IService>("SessionRoot").To<Service>()

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
await session1.DisposeAsync();
// Checks that the scoped instance is finalized
dependency1.IsDisposed.ShouldBeTrue();
        
// Disposes of session #2
await session2.DisposeAsync();
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
  Composition --|> IAsyncDisposable
  class Session {
    +Session(Composition composition)
  }
  class Program {
    +Program(FuncᐸSessionᐳ sessionFactory)
  }
  Dependency --|> IDependency : 
  Dependency --|> IAsyncDisposable : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  class Composition
  class FuncᐸSessionᐳ
  class IDependency {
    <<abstract>>
  }
  class IAsyncDisposable {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Session *--  Composition : Composition
  Program o--  "PerResolve" FuncᐸSessionᐳ : FuncᐸSessionᐳ
  Service o--  "Scoped" Dependency : IDependency
  Composition ..> Service : IService SessionRoot
  Composition ..> Program : Program ProgramRoot
  FuncᐸSessionᐳ *--  Session : Session
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition: global::System.IDisposable, global::System.IAsyncDisposable
{
  private readonly Composition _rootM04D26di;
  private readonly object _lockM04D26di;
  private object[] _disposablesM04D26di;
  private int _disposeIndexM04D26di;
  private Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Dependency _scopedM04D26di36_Dependency;
  
  public Composition()
  {
    _rootM04D26di = this;
    _lockM04D26di = new object();
    _disposablesM04D26di = new object[1];
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM04D26di = baseComposition._rootM04D26di;
    _lockM04D26di = _rootM04D26di._lockM04D26di;
    _disposablesM04D26di = new object[1];
  }
  
  public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService SessionRoot
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
    get
    {
      if (_scopedM04D26di36_Dependency == null)
      {
          lock (_lockM04D26di)
          {
              if (_scopedM04D26di36_Dependency == null)
              {
                  _scopedM04D26di36_Dependency = new Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Dependency();
                  _disposablesM04D26di[_disposeIndexM04D26di++] = _scopedM04D26di36_Dependency;
              }
          }
      }
      return new Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Service(_scopedM04D26di36_Dependency);
    }
  }
  
  public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program ProgramRoot
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
    get
    {
      var perResolveM04D26di43_Func = default(System.Func<Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Session>);
      perResolveM04D26di43_Func = new global::System.Func<Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Session>(
      [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
      () =>
      {
          Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Composition transientM04D26di2_Composition = this;
          var value_M04D26di1 = new Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Session(transientM04D26di2_Composition);
          return value_M04D26di1;
      });
      return new Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program(perResolveM04D26di43_Func);
    }
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return ResolverM04D26di<T>.Value.Resolve(this);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM04D26di<T>.Value.ResolveByTag(this, tag);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public object Resolve(global::System.Type type)
  {
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D26di[index];
    return pair.Key == type ? pair.Value.Resolve(this) : ResolveM04D26di(type, index);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x8)]
  private object ResolveM04D26di(global::System.Type type, int index)
  {
    var finish = index + _bucketSizeM04D26di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D26di[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM04D26di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    ref var pair = ref _bucketsM04D26di[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : ResolveM04D26di(type, tag, index);
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x8)]
  private object ResolveM04D26di(global::System.Type type, object? tag, int index)
  {
    var finish = index + _bucketSizeM04D26di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D26di[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    lock (_lockM04D26di)
    {
      disposeIndex = _disposeIndexM04D26di;
      _disposeIndexM04D26di = 0;
      disposables = _disposablesM04D26di;
      _disposablesM04D26di = new object[1];
      _scopedM04D26di36_Dependency = null;
    }
    
    while (disposeIndex > 0)
    {
      var instance = disposables[--disposeIndex];
      var asyncDisposableInstance = instance as global::System.IAsyncDisposable;
      if (asyncDisposableInstance != null)
      {
        try
        {
          var valueTask = asyncDisposableInstance.DisposeAsync();
          if (!valueTask.IsCompleted)
          {
            valueTask.AsTask().Wait();
          }
        }
        catch (Exception exception)
        {
          OnAsyncDisposeException(asyncDisposableInstance, exception);
        }
        continue;
      }
    }
  }
  
  partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : global::System.IDisposable;
  
  public async global::System.Threading.Tasks.ValueTask DisposeAsync()
  {
    int disposeIndex;
    object[] disposables;
    lock (_lockM04D26di)
    {
      disposeIndex = _disposeIndexM04D26di;
      _disposeIndexM04D26di = 0;
      disposables = _disposablesM04D26di;
      _disposablesM04D26di = new object[1];
      _scopedM04D26di36_Dependency = null;
    }
    
    while (disposeIndex > 0)
    {
      var instance = disposables[--disposeIndex];
      var asyncDisposableInstance = instance as global::System.IAsyncDisposable;
      if (asyncDisposableInstance != null)
      {
        try
        {
          await asyncDisposableInstance.DisposeAsync();
        }
        catch (Exception exception)
        {
          OnAsyncDisposeException(asyncDisposableInstance, exception);
        }
        continue;
      }
    }
  }
  
  partial void OnAsyncDisposeException<T>(T asyncDisposableInstance, Exception exception) where T : global::System.IAsyncDisposable;
  
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
        "  Composition --|> IAsyncDisposable\n" +
        "  class Session {\n" +
          "    +Session(Composition composition)\n" +
        "  }\n" +
        "  class Program {\n" +
          "    +Program(FuncᐸSessionᐳ sessionFactory)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  Dependency --|> IAsyncDisposable : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class Composition\n" +
        "  class FuncᐸSessionᐳ\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IAsyncDisposable {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Session *--  Composition : Composition\n" +
        "  Program o--  \"PerResolve\" FuncᐸSessionᐳ : FuncᐸSessionᐳ\n" +
        "  Service o--  \"Scoped\" Dependency : IDependency\n" +
        "  Composition ..> Service : IService SessionRoot\n" +
        "  Composition ..> Program : Program ProgramRoot\n" +
        "  FuncᐸSessionᐳ *--  Session : Session";
  }
  
  private readonly static int _bucketSizeM04D26di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM04D26di;
  
  static Composition()
  {
    var valResolverM04D26di_0000 = new ResolverM04D26di_0000();
    ResolverM04D26di<Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService>.Value = valResolverM04D26di_0000;
    var valResolverM04D26di_0001 = new ResolverM04D26di_0001();
    ResolverM04D26di<Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program>.Value = valResolverM04D26di_0001;
    _bucketsM04D26di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM04D26di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService), valResolverM04D26di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program), valResolverM04D26di_0001)
      });
  }
  
  private sealed class ResolverM04D26di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM04D26di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM04D26di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService>
  {
    public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService Resolve(Composition composition)
    {
      return composition.SessionRoot;
    }
    
    public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.SessionRoot;
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.IService.");
      }
    }
  }
  
  private sealed class ResolverM04D26di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program>
  {
    public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program Resolve(Composition composition)
    {
      return composition.ProgramRoot;
    }
    
    public Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.ProgramRoot;
        default:
          throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Lifetimes.AsyncDisposableScopeScenario.Program.");
      }
    }
  }
}
```

</blockquote></details>

