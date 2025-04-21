// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace BlazorWebAssemblyApp;

using Clock.Models;
using Clock.ViewModels;
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
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()
            // Provides the composition root for Clock view model
            .Root<IClockViewModel>(nameof(ClockViewModel))

        // Services
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
        .Bind().As(Singleton).To<WeatherForecastService>()
            // Provides the composition root for Weather Forecast service
            .Root<IWeatherForecastService>()
        .Bind().As(Singleton).To<CounterService>()
            // Provides the composition root for Counter service
            .Root<ICounterService>()

        // Infrastructure
        .Bind().To<Dispatcher>();
}