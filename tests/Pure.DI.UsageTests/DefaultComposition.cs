namespace Pure.DI.UsageTests;

internal class DefaultComposition
{
    private static void Setup() =>
        DI.Setup(nameof(DefaultComposition), CompositionKind.Global)
            .Hint(Hint.ToString, "On")
            .Hint(Hint.FormatCode, "On");
}