/*
$v=true
$p=18
$d=Async Root
*/

// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.AsyncRootScenario;

using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()

            // Specifies to use CancellationToken from the argument
            // when resolving a composition root
            .RootArg<CancellationToken>("cancellationToken")

            // Composition root
            .Root<Task<IService>>("GetMyServiceAsync");

        var composition = new Composition();

        // Resolves composition roots asynchronously
        var service = await composition.GetMyServiceAsync(CancellationToken.None);
// }
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}