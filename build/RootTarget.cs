// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

internal class RootTarget(
    RootCommand rootCommand,
    IEnumerable<IInitializable> initializables,
    Paths paths)
    : ITarget<int>
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Directory.SetCurrentDirectory(paths.SolutionDirectory);
        await new DotNetBuildServerShutdown().RunAsync(cancellationToken: cancellationToken);
        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync();
        }

        return await rootCommand.InvokeAsync(Args.ToArray());
    }
}