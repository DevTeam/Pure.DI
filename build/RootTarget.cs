// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

internal class RootTarget(
    RootCommand rootCommand,
    IEnumerable<IInitializable> initializables,
    Env env)
    : ITarget<int>
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Directory.SetCurrentDirectory(env.GetPath(PathType.SolutionDirectory));
        Environment.SetEnvironmentVariable("DOTNET_NUGET_SIGNATURE_VERIFICATION", "false");
        await new DotNetBuildServerShutdown().RunAsync(cancellationToken: cancellationToken);
        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync();
        }

        return await rootCommand.InvokeAsync(Args.ToArray());
    }
}