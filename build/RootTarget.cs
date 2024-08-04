// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

internal class RootTarget(
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

        return await rootCommand.InvokeAsync(Args.ToArray());
    }
}