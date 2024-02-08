// ReSharper disable UnusedMember.Local

namespace BlazorServerApp;

using Clock.Models;
using Clock.ViewModels;
using Models;
using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            
            // View Models
            .Bind<IClockViewModel>()
                .To<ClockViewModel>()
                // Provides the composition root for Clock view model
                .Root<IClockViewModel>("ClockViewModel")
            .Bind<IErrorViewModel>()
                .To<ErrorViewModel>()
                // Provides the composition root for Error view model
                .Root<IErrorViewModel>()

            // Services
            .Bind<ILog<TT>>().To<Log<TT>>()
            .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
            .Bind<ITimer>().As(Singleton).To<Timer>()
            .Bind<IClock>().As(PerBlock).To<SystemClock>()
            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
                // Provides the composition root for Weather Forecast service
                .Root<IWeatherForecastService>()
            .Bind<ICounterService>()
                .As(Singleton)
                .To<CounterService>()
                // Provides the composition root for Counter service
                .Root<ICounterService>()
            
            // Infrastructure
            .Bind<IDispatcher>().To<Dispatcher>();
}