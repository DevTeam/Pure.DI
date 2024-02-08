// ReSharper disable UnusedMember.Local

namespace WebAPI;

using Pure.DI;
using Controllers;
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
            // Provides the composition root for Weather Forecast controller
            .Root<WeatherForecastController>();
}