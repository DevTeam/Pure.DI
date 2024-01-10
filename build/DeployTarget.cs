// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using HostApi;
using Pure.DI;

internal class DeployTarget: Command, ITarget<int>
{
    private readonly Settings _settings;
    private readonly ITarget<IReadOnlyCollection<string>> _packTarget;

    public DeployTarget(
        Settings settings,
        [Tag(typeof(PackTarget))] ITarget<IReadOnlyCollection<string>> packTarget)
        : base("deploy", "Push NuGet packages")
    {
        _settings = settings;
        _packTarget = packTarget;
        this.SetHandler(RunAsync);
        AddAlias("d");
    }
    
    public async Task<int> RunAsync(InvocationContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var packages = await _packTarget.RunAsync(ctx);
            foreach (var package in packages)
            {
                var push = new DotNetNuGetPush()
                    .WithPackage(package)
                    .WithSources("https://api.nuget.org/v3/index.json")
                    .WithApiKey(_settings.NuGetKey);

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