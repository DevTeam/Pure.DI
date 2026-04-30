#### Unity with prefabs

Demonstrates advanced Unity integration showing how Pure.DI works with Unity prefabs and component lifecycle.


```c#
using Shouldly;
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public class ClockDigital : MonoBehaviour
{
    [SerializeField] private Text timeText;

    [Dependency] public IClockService ClockService { private get; set; }

    void FixedUpdate()
    {
        var now = ClockService.Now;
        timeText.text = now.ToString("HH:mm:ss");
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }

    bool ShowDigital { get; }

    ClockDigital ClockDigitalPrefab { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;
    [SerializeField] bool showDigital;
    [SerializeField] ClockDigital clockDigitalPrefab;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);

    public bool ShowDigital => showDigital;

    public ClockDigital ClockDigitalPrefab => clockDigitalPrefab;
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public void Dispose()
    {
        SuppressFinalize(this);
        // Perform any necessary cleanup here
    }
}

public class ClockManager : IDisposable
{
    private readonly Scope _scope;
    private readonly IClockConfig _config;

    public ClockManager(Scope scope, IClockConfig config)
    {
        _scope = scope;
        _config = config;
    }

    public void Start()
    {
        if (_config.ShowDigital)
        {
            _scope.BuildUp(Object.Instantiate(_config.ClockDigitalPrefab));
        }
    }

    public void Dispose()
    {
        SuppressFinalize(this);
        // Perform any necessary cleanup here
    }
}

public partial class Scope : MonoBehaviour
{
    [SerializeField]  ClockConfig clockConfig;

    void Setup() => DI.Setup()
        .Bind().To(() => clockConfig)
        .Bind().As(Lifetime.Singleton).To<ClockService>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    void Start()
    {
        ClockManager.Start();
    }

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
>Prefab integration with DI requires careful handling of Unity's instantiation and component initialization phases.

The following partial class will be generated:

```c#
partial class Scope: IDisposable
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif
  private object[] _disposables = new object[1];
  private int _disposeIndex;

  private ClockService? _singletonClockService63;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Clock BuildUp(Clock buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Clock transientClock665;
    Clock localBuildingInstance18 = buildingInstance;
    EnsureClockServiceExists1();
    localBuildingInstance18.ClockService = _singletonClockService63;
    transientClock665 = localBuildingInstance18;
    return transientClock665;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureClockServiceExists1()
    {
      if (_singletonClockService63 is null)
        lock (_lock)
          if (_singletonClockService63 is null)
          {
            ClockConfig transientClockConfig667 = clockConfig;
            _singletonClockService63 = new ClockService(transientClockConfig667);
            _disposables[_disposeIndex++] = _singletonClockService63;
          }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ClockDigital BuildUp(ClockDigital buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    ClockDigital transientClockDigital661;
    ClockDigital localBuildingInstance17 = buildingInstance;
    EnsureClockServiceExists();
    localBuildingInstance17.ClockService = _singletonClockService63;
    transientClockDigital661 = localBuildingInstance17;
    return transientClockDigital661;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureClockServiceExists()
    {
      if (_singletonClockService63 is null)
        lock (_lock)
          if (_singletonClockService63 is null)
          {
            ClockConfig transientClockConfig664 = clockConfig;
            _singletonClockService63 = new ClockService(transientClockConfig664);
            _disposables[_disposeIndex++] = _singletonClockService63;
          }
    }
  }

  #pragma warning disable CS0162
  [MethodImpl(MethodImplOptions.NoInlining)]
  public UnityEngine.MonoBehaviour BuildUp(UnityEngine.MonoBehaviour buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    switch (buildingInstance)
    {
      case Clock Clock2:
        return BuildUp(Clock2);
      case ClockDigital ClockDigital:
        return BuildUp(ClockDigital);
      default:
        throw new ArgumentException($"Unable to build an instance of typeof type {buildingInstance.GetType()}.", "buildingInstance");
    }
    return buildingInstance;
  }
  #pragma warning restore CS0162

  public ClockManager ClockManager
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      ClockConfig transientClockConfig670 = clockConfig;
      return new ClockManager(this, transientClockConfig670);
    }
  }

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
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new CannotResolveException($"{CannotResolveMessage} {OfTypeMessage} {type}.", type, null);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, object? tag, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

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
      _disposables = new object[1];
      _singletonClockService63 = null;
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

  private readonly static uint _bucketSize;
  private readonly static Pair<IResolver<Scope, object>>[] _buckets;

  static Scope()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<ClockManager>.Value = valResolver_0000;
    _buckets = Buckets<IResolver<Scope, object>>.Create(
      1,
      out _bucketSize,
      new Pair<IResolver<Scope, object>>[1]
      {
         new Pair<IResolver<Scope, object>>(typeof(ClockManager), valResolver_0000)
      });
  }

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

  private sealed class Resolver_0000: Resolver<ClockManager>
  {
    public override ClockManager Resolve(Scope composition)
    {
      return composition.ClockManager;
    }

    public override ClockManager ResolveByTag(Scope composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.ClockManager;

        default:
          return base.ResolveByTag(composition, tag);
      }
    }
  }
}
```


