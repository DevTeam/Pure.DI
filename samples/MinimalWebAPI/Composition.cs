using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MinimalWebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `RootBind<>()` calls.
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        // Owned is used here to dispose of all disposable instances associated with the root.
        .Root<Owned<Program>>(nameof(Root))
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}