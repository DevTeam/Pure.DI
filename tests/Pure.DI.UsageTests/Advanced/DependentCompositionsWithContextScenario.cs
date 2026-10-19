/*
$v=true
$p=7
$d=Dependent compositions with setup context
$sa=Dependent compositions
$h=This scenario shows how to pass an explicit setup context when a dependent setup uses instance members.
$h=When this occurs: you need base setup state (e.g., Unity-initialized fields) inside a dependent composition.
$h=What it solves: avoids missing instance members in dependent compositions and keeps state access explicit.
$h=How it is solved in the example: uses DependsOn(setupName, kind, name) and passes the base setup instance into the dependent composition.
$h=The context is then available in the constructor, so the dependent composition keeps a normal parameterless setup method and still reads live values from the base instance.
$f=Use this when the base setup holds state that lives outside the container - options read at start-up, a host object, or settings assigned after construction - and its bindings must see the current values.
$f=The dependent composition receives the base setup instance via a constructor argument, and the instance is stored, so assign base state before you create the dependent composition.
$f=Because the bindings call instance members, they read the value on every resolution instead of capturing it once.
$f=Prefer `SetupContextKind.Members` ([members context](dependent-compositions-with-setup-context-members.md)) when nothing extra must be created, and plain `DependsOn(setupName)` when the base setup has no instance members - otherwise DIW007 reports the instance member that would be missing.
$f=
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.DependentCompositionsWithContextScenario;

using Pure.DI;
using UsageTests;
using Shouldly;
using Xunit;
using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
        // {
        var baseContext = new BaseComposition { Settings = new AppSettings("prod", 3) };
        var composition = new Composition(baseContext);
        var service = composition.Service;
        // }
        service.Report.ShouldBe("env=prod, retries=3");
        composition.SaveClassDiagram();
    }
}

// {
interface IService
{
    string Report { get; }
}

class Service(IAppSettings settings) : IService
{
    public string Report { get; } = $"env={settings.Environment}, retries={settings.RetryCount}";
}

internal partial class BaseComposition
{
    internal AppSettings Settings { get; set; } = new("", 0);

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), Internal)
            .Bind<IAppSettings>().To(_ => Settings);
    }
}

internal partial class Composition
{
    private void Setup()
    {
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.Argument, "baseContext")
            .Bind<IService>().To<Service>()
            .Root<IService>("Service");
    }
}

record AppSettings(string Environment, int RetryCount) : IAppSettings;

interface IAppSettings
{
    string Environment { get; }

    int RetryCount { get; }
}
// }
