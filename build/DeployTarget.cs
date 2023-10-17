namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using HostApi;
using Pure.DI;

internal class DeployTarget: ITarget<int>, ICommandProvider
{
    private readonly Settings _settings;
    private readonly ITarget<IReadOnlyCollection<string>> _packTarget;

    public DeployTarget(
        Settings settings,
        [Tag(nameof(PackTarget))] ITarget<IReadOnlyCollection<string>> packTarget)
    {
        _settings = settings;
        _packTarget = packTarget;
        Command = new Command("deploy", "Push NuGet packages");
        Command.SetHandler(RunAsync);
        Command.AddAlias("d");
    }
    
    public Command Command { get; }

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

                Assertion.Succeed(push.Build());   
            }
        }
        else
        {
            Warning($"The NuGet key was not specified, the packages will not be pushed.");
        }

        return 0;
    }
}