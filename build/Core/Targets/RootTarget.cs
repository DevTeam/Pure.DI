// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class RootTarget(
    RootCommand rootCommand,
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
            return await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        }

        foreach (var error in parseResult.Errors)
        {
            Error(error.Message);
        }

        return 1;
    }
}