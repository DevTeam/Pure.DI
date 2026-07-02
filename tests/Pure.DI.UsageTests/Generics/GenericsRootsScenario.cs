/*
$v=true
$p=13
$d=Generic roots
$sa=Roots
$sa=Generics
$h=Declaring a separate root for every generic implementation gets tedious. `Roots<IExporter<TT>>("GetMy{type}")` creates a composition root for each type implementing `IExporter<TT>` that is known at compile time at the point of the call.
$h=The `{type}` placeholder in the name template is replaced with the implementation name, producing methods like `GetMyFileExporter_T<T>()` and `GetMyNetworkExporter_T<T>()`.
$f=>[!NOTE]
$f=>Generic roots enable exposing multiple generic implementations without explicitly registering each one.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.GenericsRootsScenario;

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
            // Disable Resolve methods to keep the public API minimal
            .Hint(Hint.Resolve, "Off")
            .Bind().To<JsonFormatter<TT>>()
            .Bind().To<FileExporter<TT>>()
            // Creates NetworkExporter manually,
            // just for the sake of example
            .Bind<NetworkExporter<TT>>().To(ctx => {
                ctx.Inject(out IFormatter<TT> formatter);
                return new NetworkExporter<TT>(formatter);
            })

            // Specifies to define composition roots for all types inherited from IExporter<TT>
            // available at compile time at the point where the method is called
            .Roots<IExporter<TT>>("GetMy{type}");

        var composition = new Composition();

        // fileExporter = new FileExporter<int>(new JsonFormatter<int>());
        var fileExporter = composition.GetMyFileExporter_T<int>();

        // networkExporter = new NetworkExporter<string>(new JsonFormatter<string>());
        var networkExporter = composition.GetMyNetworkExporter_T<string>();
        // }
        fileExporter.ShouldBeOfType<FileExporter<int>>();
        networkExporter.ShouldBeOfType<NetworkExporter<string>>();
        composition.SaveClassDiagram();
    }
}

// {
interface IFormatter<T>;

class JsonFormatter<T> : IFormatter<T>;

interface IExporter<T>;

class FileExporter<T>(IFormatter<T> formatter) : IExporter<T>;

class NetworkExporter<T>(IFormatter<T> formatter) : IExporter<T>;
// }