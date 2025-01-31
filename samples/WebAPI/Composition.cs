// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable RedundantUsingDirective
namespace WebAPI;

using Pure.DI;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using Pure.DI.MS;
using WeatherForecast;
using static Pure.DI.Lifetime;

internal partial class Composition : ServiceProviderFactory<Composition>
{
    static void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
        // Registers controllers as roots
        .Roots<ControllerBase>();
}