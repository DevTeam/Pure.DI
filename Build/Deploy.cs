using HostApi;

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
        if (string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            Error("NuGet key was not specified.");
            Environment.Exit(1);
        }

        var package = _build.Run();
        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            Warning("The NuGet key was not specified, the package will not be pushed.");
            var push = new DotNetNuGetPush()
                .WithPackage(package)
                .WithApiKey(_settings.NuGetKey);

            /*Assertion.Succeed(_buildRunner.Run(push));*/
            WriteLine($"Run {push}.");
        }

        return package;
    }
}