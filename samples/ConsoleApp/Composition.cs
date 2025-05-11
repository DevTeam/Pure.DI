using Pure.DI;
using static Pure.DI.Lifetime;

namespace ConsoleApp;

partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Hint(Hint.ThreadSafe, "Off")
        .Hint(Hint.ToString, "On")

        // Composition root for the console application
        .Root<Program>(nameof(Root))

        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()
        .Bind().To<ConsoleAdapter>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>();
}