// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace WebApp;

using Pure.DI;
using Pure.DI.MS;
using Controllers;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    static void Setup() => DI.Setup()
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
        // Provides the composition root for Home controller
        .Root<HomeController>();
}