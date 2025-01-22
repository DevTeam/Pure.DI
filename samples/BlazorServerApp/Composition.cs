// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace BlazorServerApp;

using Clock.Models;
using Clock.ViewModels;
using Models;
using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        // View Models
        .Bind().To<ClockViewModel>()
        // Provides the composition root for Clock view model
        .Root<IClockViewModel>(nameof(ClockViewModel))
        .Bind().To<ErrorViewModel>()
        // Provides the composition root for Error view model
        .Root<IErrorViewModel>()

        // Services
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
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