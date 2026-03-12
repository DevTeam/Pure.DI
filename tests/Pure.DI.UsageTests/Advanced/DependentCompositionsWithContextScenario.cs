/*
$v=true
$p=7
$d=Dependent compositions with setup context
$h=This scenario shows how to pass an explicit setup context when a dependent setup uses instance members.
$h=When this occurs: you need base setup state (e.g., Unity-initialized fields) inside a dependent composition.
$h=What it solves: avoids missing instance members in dependent compositions and keeps state access explicit.
$h=How it is solved in the example: uses DependsOn(setupName, kind, name) and passes the base setup instance into the dependent composition.
$f=
$f=What it shows:
$f=- Explicit setup context injection for dependent compositions.
$f=
$f=Important points:
$f=- The dependent composition receives the base setup instance via a constructor argument.
$f=
$f=Useful when:
$f=- Base setup has instance members initialized externally (e.g., Unity).
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
