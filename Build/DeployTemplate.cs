using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;

class DeployTemplate
{
    private readonly Settings _settings;
    private readonly IBuildRunner _buildRunner;
    private readonly ITeamCityWriter _teamCityWriter;

    public DeployTemplate(
        Settings settings,
        IBuildRunner buildRunner,
        ITeamCityWriter teamCityWriter)
    {
        _settings = settings;
        _buildRunner = buildRunner;
        _teamCityWriter = teamCityWriter;
    }

    public string Run()
    {
        var templatePackageVersion = Tools.GetNextVersion(new NuGetRestoreSettings("Pure.DI.Templates"), new NuGetVersion(1, 0, 0));
        var props = new[]
        {
            ("configuration", _settings.configuration),
            ("version", templatePackageVersion.ToString()!)
        };

        var pack = new DotNetPack()
            .WithProject(Path.Combine("Pure.DI.Templates", "Pure.DI.Templates.csproj"))
            .WithProps(props);

        Assertion.Succeed(_buildRunner.Run(pack));
        
        var targetPackage = Path.Combine(
            "Pure.DI.Templates",
            "bin",
            $"Pure.DI.Templates.{templatePackageVersion}.nupkg");
        
        _teamCityWriter.PublishArtifact($"{targetPackage} => .");

        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(targetPackage)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(_settings.NuGetKey);

            Assertion.Succeed(_buildRunner.Run(push));
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
        }

        return targetPackage;
    }
}