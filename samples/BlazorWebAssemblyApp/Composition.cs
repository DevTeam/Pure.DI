// ReSharper disable UnusedMember.Local

namespace BlazorWebAssemblyApp;

using Clock.Models;
using Clock.ViewModels;
using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            // Specifies not to attempt to resolve types whose fully qualified name
            // begins with Microsoft.Extensions., Microsoft.AspNetCore.
            // since ServiceProvider will be used to retrieve them.
            .Hint(Hint.OnCannotResolveContractTypeNameRegularExpression, "^Microsoft\\.(Extensions|AspNetCore)\\..+$")
            
            // View Models
            .Bind<IClockViewModel>()
                .As(Singleton)
                .To<ClockViewModel>()
                // Provides the composition root for Clock view model
                .Root<IClockViewModel>("ClockViewModel")

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