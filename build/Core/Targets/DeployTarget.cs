// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class DeployTarget(
    Settings settings,
    Commands commands,
    [Tag(typeof(PackTarget))] ITarget<IReadOnlyCollection<Package>> packTarget)
    : IInitializable, ITarget<int>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Package deployment", "deploy", "dp");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            Warning("The NuGet key was not specified, the packages will not be pushed.");
            return 0;
        }

        var packages = await packTarget.RunAsync(cancellationToken);
        foreach (var package in packages.Where(i => i.Deploy))
        {
            await new DotNetNuGetPush()
                .WithSource("https://api.nuget.org/v3/index.json")
                .WithPackage(package.Path).WithApiKey(settings.NuGetKey)
                .WithShortName($"pushing the package {package.Path}")
                .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }

        return 0;
    }
}