#### Global compositions

When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.
Use it for cross-cutting setup that must hold everywhere: hints, custom attribute aliases, a default lifetime - the settings you would otherwise copy into every composition.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.CompositionKind;

return;

class MyGlobalComposition
{
    static void Setup() =>
        DI.Setup(kind: Global)
            .Hint(Hint.ToString, "Off")
            .Hint(Hint.FormatCode, "On");
}

class MyGlobalComposition2
{
    static void Setup() =>
        DI.Setup("Some name", kind: Global)
            .Hint(Hint.ToString, "On");
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

A global setup is applied before the composition's own setup, so a later binding of the same contract overrides it and `DIW000` reports the override.
The name matters twice: it identifies the setup for `DependsOn` and it decides the order in which several global setups are applied.
>[!IMPORTANT]
>Global compositions apply to all other compositions in the project automatically, so use them carefully to avoid unintended side effects.

Prefer `CompositionKind.Internal` plus an explicit `DependsOn` ([dependent compositions](dependent-compositions.md)) when only some compositions should share the layer.



See also:

- [Dependent compositions](dependent-compositions.md)

