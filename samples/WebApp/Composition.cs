// ReSharper disable UnusedMember.Local

namespace WebApp;

using Pure.DI;
using Pure.DI.MS;
using Controllers;
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
            // Provides the composition root for Home controller
            .Root<HomeController>();
}