// ReSharper disable UnusedMember.Local
namespace WebAPI;

using Controllers;
using Pure.DI;
using Services;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IWeatherForecastService>().As(Singleton).To<WeatherForecastService>()
            .Root<WeatherForecastController>();
}