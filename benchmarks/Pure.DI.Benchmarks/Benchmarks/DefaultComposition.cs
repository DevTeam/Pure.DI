// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeMemberModifiers
namespace Pure.DI.Benchmarks.Benchmarks;

class DefaultComposition
{
    static void Setup() =>
        DI.Setup("Default", CompositionKind.Global)
            .Hint(Hint.ThreadSafe, "Off")
            .Hint(Hint.ToString, "On")
            .Hint(Hint.FormatCode, "On");
}