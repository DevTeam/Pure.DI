using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorServerApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `RootBind<>()` calls.
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}