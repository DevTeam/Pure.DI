#### Unity scene scopes

In Unity, a loaded scene is a natural lifetime boundary: services should be shared within one scene and isolated from another. This example models that with scopes — each scene gets its own scope created via the generated `SetupScope` method, `Scoped` services are unique per scene, and `Singleton` services remain shared across all scenes through the application-level composition.
Unity itself creates the `MonoBehaviour` instances, so Pure.DI only builds them up, injecting members marked with `[Dependency]` instead of using constructors.


```c#
using Shouldly;
using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

// Application scope: one root for shared singletons.
var application = new Scope("Application");

// Unity loads two scenes. Each scene has its own MonoBehaviour scope object.
var menuScene = Scope.SetupScope(application, new Scope("Menu"));
var levelScene = Scope.SetupScope(application, new Scope("Level"));

// Unity creates MonoBehaviour instances. Pure.DI only builds them up.
var menuClock1 = new Clock(menuScene);
var menuClock2 = new Clock(menuScene);
var levelClock = new Clock(levelScene);

menuClock1.Awake();
menuClock2.Awake();
levelClock.Awake();

// Same scene => same scoped dependency.
menuClock1.Session.ShouldBe(menuClock2.Session);

// Different scenes => different scoped dependencies.
menuClock1.Session.ShouldNotBe(levelClock.Session);

// Singleton dependency is still shared from the application scope.
menuClock1.ClockService.ShouldBe(levelClock.ClockService);

menuScene.Dispose();
menuClock1.Session.IsDisposed.ShouldBeTrue();
levelClock.Session.IsDisposed.ShouldBeFalse();

levelScene.Dispose();
levelClock.Session.IsDisposed.ShouldBeTrue();

application.Dispose();
menuClock1.ClockService.IsDisposed.ShouldBeTrue();

public class Clock : MonoBehaviour
{
    [SerializeField] Scope scope;

    [Dependency]
    public IClockService ClockService { get; set; }

    [Dependency]
    public IClockSession Session { get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);
}

public interface IClockService
{
    DateTime Now { get; }

    bool IsDisposed { get; }
}

public class ClockService(IClockConfig config) : IClockService, IDisposable
{
    public DateTime Now => DateTime.UtcNow + config.Offset;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public interface IClockSession
{
    string SceneName { get; }

    bool IsDisposed { get; }
}

public class ClockSession([Tag("scene name")] string sceneName) : IClockSession, IDisposable
{
    public string SceneName { get; } = sceneName;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;
    [SerializeField] string sceneName;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .Bind().To(() => clockConfig)
        .Bind("scene name").To(_ => sceneName)
        .Bind<IClockService>().As(Singleton).To<ClockService>()
        .Bind<IClockSession>().As(Scoped).To<ClockSession>()
        .Builders<MonoBehaviour>();

    void OnDestroy()
    {
        Dispose();
    }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

>[!NOTE]
>In a real Unity project the scene objects are created by Unity. The sample uses constructors only to simulate serialized references in a test.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Scope: IDisposable
{
  private Scope _root;
#if NET9_0_OR_GREATER
  private Lock _lock = new Lock();
#else
  private Object _lock = new Object();
#endif
  private object[] _disposables = new object[2];
  private int _disposeIndex;

  private ClockService? _singletonNetworkDataSource;
  private ClockSession? _scopedClockSession;

  internal static Scope SetupScope(Scope parentScope, Scope childScope)
  {
    if (Object.ReferenceEquals(parentScope, null)) throw new ArgumentNullException(nameof(parentScope));
    if (Object.ReferenceEquals(childScope, null)) throw new ArgumentNullException(nameof(childScope));
    if (Object.ReferenceEquals(parentScope, childScope)) throw new ArgumentException("The parent and child scopes must be different instances.", nameof(childScope));
    childScope._root = parentScope._root ?? parentScope;
    childScope._lock = parentScope._lock;
    childScope._disposables = new object[1];
    return childScope;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Clock BuildUp(Clock buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    var root = _root ?? this;
    Clock transientClock;
    Clock localBuildingInstance = buildingInstance;
    if (root._singletonNetworkDataSource is null)
      lock (_lock)
        if (root._singletonNetworkDataSource is null)
        {
          ClockConfig transientClockConfig = clockConfig;
          root._singletonNetworkDataSource = new ClockService(transientClockConfig);
          root._disposables[root._disposeIndex++] = root._singletonNetworkDataSource;
        }

    if (_scopedClockSession is null)
      lock (_lock)
        if (_scopedClockSession is null)
        {
          string transientString = sceneName;
          _scopedClockSession = new ClockSession(transientString);
          _disposables[_disposeIndex++] = _scopedClockSession;
        }

    localBuildingInstance.ClockService = root._singletonNetworkDataSource;
    localBuildingInstance.Session = _scopedClockSession;
    transientClock = localBuildingInstance;
    return transientClock;
  }

  #pragma warning disable CS0162
  [MethodImpl(MethodImplOptions.NoInlining)]
  public UnityEngine.MonoBehaviour BuildUp(UnityEngine.MonoBehaviour buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    if (TryBuildUp(buildingInstance))
    {
      return buildingInstance;
    }
    throw new ArgumentException($"Unable to build an instance of typeof type {buildingInstance.GetType()}.", "buildingInstance");
  }
  #pragma warning restore CS0162

  #pragma warning disable CS0162
  [MethodImpl(MethodImplOptions.NoInlining)]
  public bool TryBuildUp(UnityEngine.MonoBehaviour buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    switch (buildingInstance)
    {
      case Clock Clock3:
        BuildUp(Clock3);
        return true;
      default:
        return false;
    }
    return false;
  }
  #pragma warning restore CS0162

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>()
  {
    return Resolver<T>.Value.Resolve(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>(object? tag)
  {
    return Resolver<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type)
  {
    throw new CannotResolveException($"{CannotResolveMessage} {OfTypeMessage} {type}.", type, null);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    throw new CannotResolveException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.", type, tag);
  }

  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    lock (_lock)
    {
      disposeIndex = _disposeIndex;
      _disposeIndex = 0;
      disposables = _disposables;
      _disposables = new object[2];
      _singletonNetworkDataSource = null;
      _scopedClockSession = null;
    }

    while (disposeIndex-- > 0)
    {
      switch (disposables[disposeIndex])
      {
        case IDisposable disposableInstance:
          try
          {
            disposableInstance.Dispose();
          }
          catch (Exception exception)
          {
            OnDisposeException(disposableInstance, exception);
          }
          break;
      }
    }
  }

  partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : IDisposable;

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Scope, T>
  {
    public static IResolver<Scope, T> Value = new Resolver<T>();

    public virtual T Resolve(Scope composite)
    {
      throw new CannotResolveException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.", typeof(T), null);
    }

    public virtual T ResolveByTag(Scope composite, object tag)
    {
      throw new CannotResolveException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.", typeof(T), tag);
    }
  }
}
```

</details>


See also:

- [Unity Basics](unity-basics.md)
- [Scope](scope.md)

