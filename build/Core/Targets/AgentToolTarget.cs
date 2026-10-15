namespace Build.Core.Targets;

class AgentToolTarget(Settings settings, Commands commands, Env env) : IInitializable, ITarget<Package>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Create Pure.DI.Agent .NET tool package", "agent-tool");

    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var version = settings.NextVersion;
        var outputDirectory = env.GetPath(PathType.PackagesDirectory);
        await new DotNetPack()
            .WithProject(Path.Combine("src", "Pure.DI.Agent", "Pure.DI.Agent.csproj"))
            .WithProps([
                ("configuration", settings.Configuration),
                ("version", version.ToString())
            ])
            .WithOutput(outputDirectory)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var path = Path.Combine(outputDirectory, $"Pure.DI.Agent.{version}.nupkg");
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
            throw new InvalidOperationException($"The Pure.DI.Agent package is missing or empty: {path}");
        return new Package(path, true, version);
    }
}
