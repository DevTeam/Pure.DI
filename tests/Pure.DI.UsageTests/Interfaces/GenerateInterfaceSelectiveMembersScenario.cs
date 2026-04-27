/*
$v=true
$p=6
$d=Control generated interfaces by members
$h=This example shows selective member inclusion and IgnoreInterface priority when generating interfaces.
$f=The example shows how to:
$f=- Generate interface members selectively
$f=- Keep class-level generation settings
$f=- Exclude explicitly ignored members from all generated interfaces
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interfaces.GenerateInterfaceSelectiveMembersScenario;

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
        DI.Setup(nameof(Composition))
            .Bind().To<ProfileService>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Summary.ShouldBe("42:Ada");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IProfileReader;

public partial interface IProfileWriter;

public class ProfileService : IProfileReader, IProfileWriter
{
    [GenerateInterface(interfaceName: nameof(IProfileReader))]
    public int GetId() => 42;

    [GenerateInterface(interfaceName: nameof(IProfileWriter))]
    public void Rename(string name)
    {
    }

    [GenerateInterface(interfaceName: nameof(IProfileWriter))]
    [IgnoreInterface]
    public string Secret() => "hidden";

    [GenerateInterface(interfaceName: nameof(IProfileReader))]
    public string GetName() => "Ada";
}

public class App(IProfileReader reader)
{
    public string Summary { get; } = $"{reader.GetId()}:{reader.GetName()}";
}
// }
