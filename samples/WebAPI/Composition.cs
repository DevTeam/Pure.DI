using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebAPI;

partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}