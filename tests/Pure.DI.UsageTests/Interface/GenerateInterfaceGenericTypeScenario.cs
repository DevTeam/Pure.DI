/*
$v=true
$p=9
$d=Generate interface for a generic type
$h=This example shows how to generate an interface from a generic class and use it with Pure.DI.
$f=The example shows how to:
$f=- Generate an interface for a generic class
$f=- Preserve generic constraints
$f=- Bind a closed generic type
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interface.GenerateInterfaceGenericTypeScenario;

using Pure.DI.UsageTests;
using Pure.DI;
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
        DI.Setup(nameof(Composition))
            .Bind().To<Repository<Message>>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Message.ShouldBe("hello");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IRepository<TItem>;

[GenerateInterface]
public class Repository<TItem>: IRepository<TItem>
    where TItem : class, new()
{
    public TItem Current { get; set; } = new();

    public TItem Create() => new();
}

public class Message
{
    public string Text { get; set; } = "hello";
}

public class App(IRepository<Message> repository)
{
    public string Message { get; } = repository.Current.Text;
}
// }
