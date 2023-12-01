// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace Pure.DI.Benchmarks.Benchmarks;

internal class BaseSetup
{
    private static void SetupDI() =>
        DI.Setup("", CompositionKind.Global)
            .Hint(Hint.ThreadSafe, "Off")
            .Hint(Hint.FormatCode, "On")
            .Hint(Hint.ToString, "On");
}