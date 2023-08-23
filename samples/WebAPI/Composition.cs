// ReSharper disable UnusedMember.Local
namespace WebAPI;

using System.Diagnostics;
using Controllers;
using Pure.DI;
using Services;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    [Conditional("DI")]
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IWeatherForecastService>().As(Singleton).To<WeatherForecastService>()
            .Root<WeatherForecastController>();
}