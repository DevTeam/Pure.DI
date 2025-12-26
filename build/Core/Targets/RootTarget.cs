// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class RootTarget(
    RootCommand rootCommand,
    Settings settings,
    IEnumerable<IInitializable> initializables)
    : ITarget<int>
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync(cancellationToken);
        }

        var parseResult = rootCommand.Parse(Args);
        if (!parseResult.Errors.Any())
        {
            Summary(new Text("Configuration: "), new Text(settings.Configuration, Color.Details));
            Summary(new Text("Version range: "), new Text(settings.VersionRange.ToString(), Color.Details));
            Summary(new Text("Current version: "), new Text(settings.CurrentVersion.ToString(), Color.Details));
            Summary(new Text("Next version: "), new Text(settings.NextVersion.ToString(), Color.Details));
            return await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        }

        foreach (var error in parseResult.Errors)
        {
            Error(error.Message);
        }

        return 1;
    }
}