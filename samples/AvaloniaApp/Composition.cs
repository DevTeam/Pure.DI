using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}