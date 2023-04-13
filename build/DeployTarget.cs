namespace Build;

using System.CommandLine.Invocation;
using HostApi;
using Pure.DI;

internal class DeployTarget: ITarget<int>
{
    private readonly Settings _settings;
    private readonly ITarget<string> _packTarget;

    public DeployTarget(
        Settings settings,
        [Tag("pack")] ITarget<string> packTarget)
    {
        _settings = settings;
        _packTarget = packTarget;
    }

    public async Task<int> RunAsync(InvocationContext ctx)
    {
        var package = await _packTarget.RunAsync(ctx);
        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(package)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(_settings.NuGetKey);

            Assertion.Succeed(push.Build());
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {package} will not be pushed.");
        }

        return 0;
    }
}