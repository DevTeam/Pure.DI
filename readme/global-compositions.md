#### Global compositions

When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.


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

>[!IMPORTANT]
>Global compositions apply to all other compositions in the project automatically, so use them carefully to avoid unintended side effects.



See also:

- [Dependent compositions](dependent-compositions.md)

