namespace Build.Core;

class DotNetEnv(Env env) : IInitializable
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Directory.SetCurrentDirectory(env.GetPath(PathType.SolutionDirectory));
        Environment.SetEnvironmentVariable("DOTNET_NUGET_SIGNATURE_VERIFICATION", "false");
        await new DotNetBuildServerShutdown().RunAsync(cancellationToken: cancellationToken);
    }
}