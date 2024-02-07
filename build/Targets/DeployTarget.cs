// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Targets;

internal class DeployTarget(
    Settings settings,
    ICommands commands,
    [Tag(typeof(PackTarget))] ITarget<IReadOnlyCollection<string>> packTarget)
    : IInitializable, ITarget<int>
{
    public Task InitializeAsync() => commands.Register(
        this, 
        "Deploys packages",
        "deploy",
        "dp");
    
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Info("Deployment");
        if (!string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            var packages = await packTarget.RunAsync(cancellationToken);
            foreach (var package in packages)
            {
                var push = new DotNetNuGetPush()
                    .WithPackage(package)
                    .WithSources("https://api.nuget.org/v3/index.json")
                    .WithApiKey(settings.NuGetKey);

                push.Build().Succeed();   
            }
        }
        else
        {
            Warning("The NuGet key was not specified, the packages will not be pushed.");
        }

        return 0;
    }
}