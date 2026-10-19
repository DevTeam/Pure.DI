/*
$v=true
$p=11
$d=Manually started tasks
$sa=Task
$h=By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.
$h=This is the pattern for a service that wants to start the work itself, for example at an exact moment during its own construction.
$f=>[!IMPORTANT]
$f=>The method `Inject()`cannot be used outside of the binding setup.
$f=
$f=Overriding `Task<TT>` replaces the default auto-start binding for every task in the composition, so the consumer decides when `Start` is called. Here `DashboardService` starts the task in its constructor and awaits it later in `LoadAsync`, which overlaps loading with the rest of the initialization.
$f=An unstarted task that nobody starts never completes, and `await` on it waits forever: the responsibility moves to the consumer, and that is the trade-off of the override. Keep the override narrow — or use [task](task.md) with automatic start — when only one service needs manual control.
$f=The `CancellationToken` root argument is what makes the task cancellable; without it the binding falls back to `CancellationToken.None` and the manual start cannot be interrupted.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Global

namespace Pure.DI.UsageTests.BCL.ManualTaskScenario;

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
            // Overrides the default binding that performs an auto-start of a task
            // when it is created. This binding will simply create the task.
            // The start will be handled by the consumer.
            .Bind<Task<TT>>().To(ctx => {
                ctx.Inject(ctx.Tag, out Func<TT> factory);
                ctx.Inject(out CancellationToken cancellationToken);
                return new Task<TT>(factory, cancellationToken);
            })
            // Specifies to use CancellationToken from the composition root argument,
            // if not specified, then CancellationToken.None will be used
            .RootArg<CancellationToken>("cancellationToken")
            .Bind<IUserPreferences>().To<UserPreferences>()
            .Bind<IDashboardService>().To<DashboardService>()

            // Composition root
            .Root<IDashboardService>("GetDashboard");

        var composition = new Composition();
        using var cancellationTokenSource = new CancellationTokenSource();

        // Creates a composition root with the CancellationToken passed to it
        var dashboard = composition.GetDashboard(cancellationTokenSource.Token);
        await dashboard.LoadAsync(cancellationTokenSource.Token);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IUserPreferences
{
    ValueTask LoadAsync(CancellationToken cancellationToken);
}

class UserPreferences : IUserPreferences
{
    public ValueTask LoadAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IDashboardService
{
    Task LoadAsync(CancellationToken cancellationToken);
}

class DashboardService : IDashboardService
{
    private readonly Task<IUserPreferences> _preferencesTask;

    public DashboardService(Task<IUserPreferences> preferencesTask)
    {
        _preferencesTask = preferencesTask;
        // The task is started manually in the constructor.
        // This allows the loading of preferences to begin immediately in the background,
        // while the service continues its initialization.
        _preferencesTask.Start();
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        // Wait for the preferences loading task to complete
        var preferences = await _preferencesTask;
        await preferences.LoadAsync(cancellationToken);
    }
}
// }