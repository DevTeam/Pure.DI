/*
$v=true
$p=10
$d=Dependent compositions with setup context root argument
$sa=Dependent compositions with setup context
$h=This scenario shows how to pass an explicit setup context as a root argument.
$h=When this occurs: you need external state from the base setup but cannot use a constructor (e.g., Unity MonoBehaviour).
$h=What it solves: keeps the dependent composition safe while avoiding constructor arguments.
$h=How it is solved in the example: uses DependsOn(..., SetupContextKind.RootArgument, name) and passes the base setup instance to the root method.
$h=The context is a parameter of the generated root, so one composition instance can serve several calls with different base state.
$f=Use this when the base state is only known at the moment of a resolution, for example when a long-lived host creates a short-lived scope per screen or per request.
$f=The composition itself can still be created with a parameterless constructor, so a host that instantiates it by convention keeps working.
$f=The parameter appears on every root of that composition that needs the base setup, which makes the call site explicit about what it passes.
$f=Prefer `SetupContextKind.Argument` ([setup context](dependent-compositions-with-setup-context.md)) when the state is fixed for the composition lifetime, so callers of the roots do not have to thread the context through.
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
namespace Pure.DI.UsageTests.Advanced.DependentCompositionsWithRootArgumentScenario;

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
        var baseContext = new BaseComposition { Settings = new AppSettings("staging", 2) };
        var composition = new Composition();
        var service = composition.Service(baseContext: baseContext);
        // }
        service.Report.ShouldBe("env=staging, retries=2");
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
        // Resolve = Off
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.RootArgument, "baseContext")
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
