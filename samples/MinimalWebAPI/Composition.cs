// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace MinimalWebAPI;

using Pure.DI;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    void Setup() => DI.Setup()
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
        .Root<IWeatherForecastService>()

        // Application composition root
        .Root<Program>(nameof(Root));
}