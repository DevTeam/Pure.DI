#### Unity

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnityApp)

This example shows how to use Pure.DI in a [Unity](https://unity.com/) application, including object graph generation for regular services and build-up support for `MonoBehaviour` instances managed by the engine.

> [!TIP]
> Unity creates `MonoBehaviour` instances itself, so Pure.DI uses `BuildUp` methods for scene objects and regular roots for services. The sample also shows parent/child scene scopes with scoped services.

![Unity](https://cdn.sanity.io/images/fuvbjjlp/production/01c082f3046cc45548249c31406aeffd0a9a738e-296x100.png)

The definition of the composition is in [Scope.cs](/samples/UnityApp/Assets/Scripts/Scope.cs). This class sets up how the object graphs will be created for the application. Remember to define builders for types derived from `MonoBehaviour`:

```c#
internal class ClocksComposition
{
    [SerializeField] private ClockConfig clockConfig;

    void Setup() => DI.Setup(kind: CompositionKind.Internal)
        .Transient(() => clockConfig)
        .Singleton<ClockService>();
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] private Scope parentScope;

    private bool isReady;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .DependsOn(nameof(ClocksComposition), SetupContextKind.Members)
        .Bind<IClockSession>().As(Lifetime.Scoped).To<ClockSession>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    public void EnsureReady()
    {
        if (isReady)
        {
            return;
        }

        if (parentScope != null && !ReferenceEquals(parentScope, this))
        {
            parentScope.EnsureReady();
            SetupScope(parentScope, this);
        }

        isReady = true;
    }

    void Awake()
    {
        EnsureReady();
    }

    void Start()
    {
        EnsureReady();
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
```

Use `EnsureReady()` before resolving or building scene objects. It initializes the parent scope first and prevents accidental self-parenting from creating recursive scope setup.

Advantages over classical DI container libraries:
- No performance impact or side effects when creating object graphs.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to additional assemblies.
- Since the generated code uses primitive language constructs to create object graphs and does not use any libraries, you can debug the object graph code as regular code in your application.

For types derived from `MonoBehaviour`, a `BuildUp` composition method will be generated. This method looks like:

```c#
private ClockService _singletonClockService;
[SerializeField] private ClockConfig clockConfig;
    
public global::Clock BuildUp(global::Clock buildingInstance)
{
    if (buildingInstance is null) throw new global::System.ArgumentNullException(nameof(buildingInstance));
    
    if (_singletonClockService is null)
    {
        _singletonClockService = new global::ClockService(clockConfig);
        _disposables45d[_disposeIndex45d++] = _singletonClockService;
    }
    
    buildingInstance.ClockService = _singletonClockService;
    return transientClock;
}
```

A single instance of the _Composition_ class is defined as a public property `Composition.Shared`. It provides a common composition for building classes based on `MonoBehaviour`.

An [example](/samples/UnityApp/Assets/Scripts/Clock.cs) of such a class might look like this:

```c#
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

    void Awake()
    {
        scope.BuildUp(this);
    }

    void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}
```

The Unity project should reference the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

The Unity example uses the Unity editor version 6000.0.35f1
