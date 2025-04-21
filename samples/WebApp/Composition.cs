// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable RedundantUsingDirective
namespace WebApp;

using Pure.DI;
using Pure.DI.MS;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using WeatherForecast;
using static Pure.DI.Lifetime;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    static void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)
        .Bind().As(Singleton).To<WeatherForecastService>()
        // Registers controllers as roots
        .Roots<Controller>();
}