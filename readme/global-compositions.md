#### Global compositions

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/GlobalCompositionsScenario.cs)

When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.


```c#
class MyGlobalComposition
{
    void Setup() =>
        DI.Setup(kind: CompositionKind.Global)
            .Hint(Hint.ToString, "Off")
            .Hint(Hint.FormatCode, "Off");
}

class MyGlobalComposition2
{
    void Setup() =>
        DI.Setup(kind: CompositionKind.Global)
            .Hint(Hint.ToString, "On");
}
```



