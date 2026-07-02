/*
$v=true
$p=11
$d=Global compositions
$sa=Dependent compositions
$h=When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.
$f=>[!IMPORTANT]
$f=>Global compositions apply to all other compositions in the project automatically, so use them carefully to avoid unintended side effects.
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