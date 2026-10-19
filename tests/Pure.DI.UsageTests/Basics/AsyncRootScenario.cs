/*
$v=true
$p=29
$d=Async Root
$sa=Task
$h=A composition root can be asynchronous: declare it as `Root<Task<IService>>(...)` and _Pure.DI_ generates a root method you can `await`.
$h=This is useful when building the object graph is costly and you don't want to block the caller.
$h=Add `RootArg<CancellationToken>("cancellationToken")` to pass a cancellation token that is used when resolving the root.
$f=>[!NOTE]
$f=>Async roots are useful when you need to perform asynchronous initialization or when your services require async creation.
$f=
$f=The root is a method returning `Task<T>`, so it is called like any other async API: `await composition.GetBackupServiceAsync(token)`.
$f=The token is a normal root argument here, which is why such a composition keeps `Resolve = Off`: a dynamic resolve call cannot supply the token.
$f=Prefer a synchronous root when the constructors are synchronous - an async root adds a state machine to an otherwise allocation-free path.
$r=Shouldly
*/

// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.AsyncRootScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IFileStore>().To<FileStore>()
            .Bind<IBackupService>().To<BackupService>()

            // Specifies to use CancellationToken from the argument
            // when resolving a composition root
            .RootArg<CancellationToken>("cancellationToken")

            // Composition root
            .Root<Task<IBackupService>>("GetBackupServiceAsync");

        var composition = new Composition();

        // Resolves composition roots asynchronously
        var service = await composition.GetBackupServiceAsync(CancellationToken.None);
// }
        service.ShouldBeOfType<BackupService>();
        composition.SaveClassDiagram();
    }
}

// {
interface IFileStore;

class FileStore : IFileStore;

interface IBackupService;

class BackupService(IFileStore fileStore) : IBackupService;
// }