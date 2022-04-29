using HostApi;
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ArrangeTypeModifiers

class Deploy
{
    private readonly Settings _settings;
    private readonly IBuildRunner _buildRunner;
    private readonly Build _build;

    public Deploy(
        Settings settings,
        IBuildRunner buildRunner,
        Build build)
    {
        _settings = settings;
        _buildRunner = buildRunner;
        _build = build;
    }

    public string Run()
    {
        var package = _build.Run();
        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(package)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(_settings.NuGetKey);

            Assertion.Succeed(_buildRunner.Run(push));
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {package} will not be pushed.");
        }

        return package;
    }
}