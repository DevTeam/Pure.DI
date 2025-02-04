#### Unity

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnityApp)

This example demonstrates the creation of a [Unity](https://unity.com/) application in the pure DI paradigm using the _Pure.DI_ code generator.

![Unity](https://cdn.sanity.io/images/fuvbjjlp/production/01c082f3046cc45548249c31406aeffd0a9a738e-296x100.png)

The definition of the composition is in [Composition.cs](/samples/UnityApp/Assets/Scripts/Composition.cs). This class setups how the composition of objects will be created for the application. Don't forget to define builders for types inherited from `MonoBehaviour`:

```csharp
using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();

    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ClockService>()
        .Builders<MonoBehaviour>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

For types inherited from `MonoBehaviour`, a `BuildUp` composition method will be generated. This method will look as follows:

```csharp
[CompilerServices.MethodImpl(MethodImplOptions.AggressiveInlining)]
public Clock BuildUp(Clock buildingInstance)
{
    if (buildingInstance is null) 
        throw new ArgumentNullException(nameof(buildingInstance));

    if (_clockService is null)
        lock (_lock)
            if (_clockService is null)
                _clockService = new ClockService();

    buildingInstance.ClockService = _clockService;
    return buildingInstance;
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

    [SerializeField]
    private Transform hoursPivot;
    
    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
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

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |

The Unity example uses the Unity editor version 6000.0.35f1
