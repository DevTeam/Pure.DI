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
            // Specifies not to attempt to resolve types whose fully qualified name
            // begins with Microsoft.Extensions., Microsoft.AspNetCore.
            // since ServiceProvider will be used to retrieve them.
            .Hint(Hint.OnCannotResolveContractTypeNameRegularExpression, "^Microsoft\\.(Extensions|AspNetCore)\\..+$")

            .Bind<IWeatherForecastService>()
                .As(Singleton)
                .To<WeatherForecastService>()
            // Provides the composition root for Weather Forecast controller
            .Root<WeatherForecastController>();
}