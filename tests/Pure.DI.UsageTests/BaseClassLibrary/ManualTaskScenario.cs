/*
$v=true
$p=4
$d=Manually started tasks
$h=By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
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
        // This hint indicates to not generate methods such as Resolve
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