using Pure.DI;
using System.Diagnostics;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();

    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ClockService>()
        .Builder<Clock>();
}
