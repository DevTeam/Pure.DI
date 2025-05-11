using Pure.DI;
using static Pure.DI.Lifetime;

namespace WpfAppNetCore;

partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WpfDispatcher>();
}