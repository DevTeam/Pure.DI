using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<MauiDispatcher>();
}