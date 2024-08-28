// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeMemberModifiers
namespace Pure.DI.Benchmarks.Benchmarks;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class DefaultComposition
{
    void Setup() =>
        DI.Setup("Default", CompositionKind.Global)
            .Hint(Hint.ThreadSafe, "Off")
            .Hint(Hint.ToString, "On")
            .Hint(Hint.FormatCode, "On");
}