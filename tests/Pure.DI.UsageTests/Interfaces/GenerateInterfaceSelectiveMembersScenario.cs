/*
$v=true
$p=6
$d=Control generated interfaces by members
$h=This example shows selective member inclusion and IgnoreInterface priority when generating interfaces. A class can therefore expose a narrow reader contract and a separate writer contract over one implementation.
$f=The example shows how to:
$f=- Generate interface members selectively
$f=- Keep class-level generation settings
$f=- Exclude explicitly ignored members from all generated interfaces
$f=
$f=Interface segregation becomes cheap this way: the implementation stays one class, while consumers receive only the operations they need. It also documents intent member by member instead of forcing a split of the class.
$f=A member can belong to several interfaces, and `[IgnoreInterface]` wins over any inclusion on the same member. Use it for members that must never leave the implementation, not for temporary exclusions.
$f=>[!WARNING]
$f=>Forgetting the attribute on a new public member means it is not part of any generated interface, and the consumer that needs it fails to compile. Add the attribute together with the member.
$f=
$f=Prefer this over several classes when the members share state; prefer [Generate several interfaces from one class](generate-several-interfaces-from-one-class.md) when whole groups are stable.
$r=Shouldly
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
