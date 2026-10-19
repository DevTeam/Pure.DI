/*
$v=true
$p=11
$d=Global compositions
$sa=Dependent compositions
$h=When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.
$h=Use it for cross-cutting setup that must hold everywhere: hints, custom attribute aliases, a default lifetime - the settings you would otherwise copy into every composition.
$f=A global setup is applied before the composition's own setup, so a later binding of the same contract overrides it and `DIW000` reports the override.
$f=The name matters twice: it identifies the setup for `DependsOn` and it decides the order in which several global setups are applied.
$f=>[!IMPORTANT]
$f=>Global compositions apply to all other compositions in the project automatically, so use them carefully to avoid unintended side effects.
$f=
$f=Prefer `CompositionKind.Internal` plus an explicit `DependsOn` ([dependent compositions](dependent-compositions.md)) when only some compositions should share the layer.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.GlobalCompositionsScenario;

using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;

//# return;
// }

// {
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
// }