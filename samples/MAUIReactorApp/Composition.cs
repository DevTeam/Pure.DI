using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIReactorApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Builder<>()` calls.
    private static void Setup() => DI.Setup()
        // Roots
        .Root<Composition>()

        // Builders
        .Builder<ClockPage>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<MauiDispatcher>();
}