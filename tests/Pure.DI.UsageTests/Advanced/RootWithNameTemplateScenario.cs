/*
$v=true
$p=2
$d=Root with name template
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.RootWithNameTemplateScenario;

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
        DI.Setup("Composition")
            .Root<Service>("My{type}");

        var composition = new Composition();
        var service = composition.MyService;
// }
        composition.SaveClassDiagram();
    }
}

// {
class Dependency;

class Service(Dependency dependency);
// }