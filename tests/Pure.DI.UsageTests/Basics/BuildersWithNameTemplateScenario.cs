/*
$v=true
$p=9
$d=Builders with a name template
$h=Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$f=The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
        // {
        DI.Setup(nameof(Composition))
            .Bind().To(Guid.NewGuid)
            .Bind().To<WiFi>()
            // Creates a builder based on the name template
            // for each type inherited from IDevice.
            // These types must be available at this point in the code.
            .Builders<IDevice>("Install{type}");

        var composition = new Composition();

        var webcam = composition.InstallWebcam(new Webcam());
        webcam.Id.ShouldNotBe(Guid.Empty);
        webcam.Network.ShouldBeOfType<WiFi>();

        var thermostat = composition.InstallThermostat(new Thermostat());
        thermostat.Id.ShouldBe(Guid.Empty);
        thermostat.Network.ShouldBeOfType<WiFi>();

        // Uses a common method to build an instance
        IDevice device = new Webcam();
        device = composition.InstallIDevice(device);
        device.ShouldBeOfType<Webcam>();
        device.Id.ShouldNotBe(Guid.Empty);
        device.Network.ShouldBeOfType<WiFi>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface INetwork;

class WiFi : INetwork;

interface IDevice
{
    Guid Id { get; }

    INetwork? Network { get; }
}

record Webcam : IDevice
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public INetwork? Network { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Thermostat : IDevice
{
    public Guid Id => Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public INetwork? Network { get; set; }
}
// }