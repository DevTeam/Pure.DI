// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Benchmarks;

internal class DefaultComposition
{
    private static void Setup() =>
        DI.Setup(nameof(DefaultComposition), CompositionKind.Global)
            .Hint(Hint.ThreadSafe, "Off")
            .Hint(Hint.ToString, "On")
            .Hint(Hint.FormatCode, "On");
}