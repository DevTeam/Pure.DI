// ReSharper disable UnusedMember.Local

namespace BlazorWebAssemblyApp;

using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .DependsOn(Base)
            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
                .Root<IWeatherForecastService>()
            .Bind<ICounterService>()
                .To<CounterService>()
                .Root<ICounterService>();
}