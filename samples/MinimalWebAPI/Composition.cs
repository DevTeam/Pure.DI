// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace MinimalWebAPI;

using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
            .Root<IWeatherForecastService>()

        // Application composition root
        .Root<Program>(nameof(Root));
}