/*
$v=true
$p=17
$d=Service collection
$sa=Service provider
$sa=Keyed service provider
$h=The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an `IServiceCollection` object:
$f=>[!NOTE]
$f=>This enables integration with _Microsoft.Extensions.DependencyInjection_, allowing you to leverage both DI systems together.
$f=
$f=`ServiceProviderFactory<Composition>` is what a web host or a Blazor application consumes: the composition builds a real `IServiceCollection`, so registration is owned by the compile-time graph while the host resolves services at run time. This is the supported way to plug Pure.DI into framework code that requires `IServiceCollection`.
$f=Each root becomes a registration, and a tagged root becomes a keyed registration, which is how `GetRequiredKeyedService("LivingRoom")` returns the same instance the thermostat holds. Test both paths when a root's tag changes, because the tag is the service key.
$f=>[!IMPORTANT]
$f=>A composition used as a service provider factory must keep its resolve methods enabled; `Hint.Resolve = Off` removes the methods the generated registrations call.
$r=Pure.DI.MS;Shouldly;Microsoft.Extensions.DependencyInjection
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.BCL.ServiceCollectionScenario;

using Microsoft.Extensions.DependencyInjection;
using MS;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var serviceCollection = composition.ServiceCollection;
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var thermostat = serviceProvider.GetRequiredService<IThermostat>();
        var sensor = serviceProvider.GetRequiredKeyedService<ISensor>("LivingRoom");
        thermostat.Sensor.ShouldBe(sensor);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface ISensor;

class TemperatureSensor : ISensor;

interface IThermostat
{
    ISensor Sensor { get; }
}

class Thermostat([Tag("LivingRoom")] ISensor sensor) : IThermostat
{
    public ISensor Sensor { get; } = sensor;
}

partial class Composition : ServiceProviderFactory<Composition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    static void Setup() =>
        DI.Setup()
            .Bind<ISensor>("LivingRoom").As(Lifetime.Singleton).To<TemperatureSensor>()
            .Bind<IThermostat>().To<Thermostat>()
            .Root<ISensor>(tag: "LivingRoom")
            .Root<IThermostat>();
}
// }