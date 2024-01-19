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
                .Root<IClockViewModel>("ClockViewModel")
            .Bind<IErrorViewModel>()
                .To<ErrorViewModel>()
                .Root<IErrorViewModel>()

            // Services
            .Bind<ILog<TT>>().To<Log<TT>>()
            .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
            .Bind<ITimer>().As(Singleton).To<Timer>()
            .Bind<IClock>().As(PerBlock).To<SystemClock>()
            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
                .Root<IWeatherForecastService>()
            .Bind<ICounterService>()
                .As(Singleton)
                .To<CounterService>()
                .Root<ICounterService>()
            
            // Infrastructure
            .Bind<IDispatcher>().To<Dispatcher>();
}