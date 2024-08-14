// ReSharper disable UnusedMember.Local

// ReSharper disable ArrangeTypeMemberModifiers

namespace WebAPI;

using Pure.DI;
using Controllers;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    static void Setup() => DI.Setup()
        .DependsOn(Base)
        // Specifies not to attempt to resolve types whose fully qualified name
        // begins with Microsoft.Extensions., Microsoft.AspNetCore.
        // since ServiceProvider will be used to retrieve them.
        .Hint(
            Hint.OnCannotResolveContractTypeNameRegularExpression,
            @"^Microsoft\.(Extensions|AspNetCore)\..+$")
        .Bind().As(Singleton).To<WeatherForecastService>()
        // Provides the composition root for Weather Forecast controller
        .Root<WeatherForecastController>();
}