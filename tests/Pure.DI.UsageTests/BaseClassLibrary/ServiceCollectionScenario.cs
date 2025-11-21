/*
$v=true
$p=98
$d=Service collection
$h=The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an _IServiceCollection_ object:
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