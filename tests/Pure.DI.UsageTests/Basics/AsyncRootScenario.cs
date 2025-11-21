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
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // This hint indicates to not generate methods such as Resolve
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