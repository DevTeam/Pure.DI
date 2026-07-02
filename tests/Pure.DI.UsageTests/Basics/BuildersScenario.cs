/*
$v=true
$p=23
$d=Builders
$sa=Builder
$sa=Builders with a name template
$h=Sometimes you need builders for all types derived from `T` that are known at compile time.
$f=Important Notes:
$f=- The default builder method name is `BuildUp`
$f=- The first argument to the builder method is always the instance to be built
$f=- `Builders<T>` also generates `TryBuildUp` for safe build-up when the runtime subtype may be unknown
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Basics.BuildersScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To(Guid.NewGuid)
            .Bind().To<PlutoniumBattery>()
            // Creates a builder for each type inherited from IRobot.
            // These types must be available at this point in the code.
            .Builders<IRobot>("BuildUp", filter: "*Bot");

        var composition = new Composition();

        var cleaner = composition.BuildUp(new CleanerBot());
        cleaner.Token.ShouldNotBe(Guid.Empty);
        cleaner.Battery.ShouldBeOfType<PlutoniumBattery>();

        var guard = composition.BuildUp(new GuardBot());
        guard.Token.ShouldBe(Guid.Empty);
        guard.Battery.ShouldBeOfType<PlutoniumBattery>();

        // Uses a common method to build an instance
        IRobot robot = new CleanerBot();
        robot = composition.BuildUp(robot);
        robot.ShouldBeOfType<CleanerBot>();
        robot.Token.ShouldNotBe(Guid.Empty);
        robot.Battery.ShouldBeOfType<PlutoniumBattery>();

        // Uses a safe common method when the runtime subtype may be unknown.
        var externalRobot = new ExternalRobot();
        composition.TryBuildUp(externalRobot).ShouldBeFalse();
        externalRobot.Battery.ShouldBeNull();

        // The strict builder still throws for unknown runtime subtypes.
        Should.Throw<ArgumentException>(() => composition.BuildUp(externalRobot));
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IBattery;

class PlutoniumBattery : IBattery;

interface IRobot
{
    Guid Token { get; }

    IBattery? Battery { get; }
}

record CleanerBot : IRobot
{
    public Guid Token { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IBattery? Battery { get; set; }

    [Dependency]
    public void SetToken(Guid token) => Token = token;
}

record GuardBot : IRobot
{
    public Guid Token => Guid.Empty;

    [Dependency]
    public IBattery? Battery { get; set; }
}

record ExternalRobot : IRobot
{
    public Guid Token => Guid.Empty;

    public IBattery? Battery => null;
}
// }
