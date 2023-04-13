// ReSharper disable StringLiteralTypo
namespace Build;

using System.CommandLine.Invocation;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;

internal class TemplateTarget: ITarget<string>
{
    private readonly Settings _settings;
    private readonly ITeamCityWriter _teamCityWriter;

    public TemplateTarget(
        Settings settings,
        ITeamCityWriter teamCityWriter)
    {
        _settings = settings;
        _teamCityWriter = teamCityWriter;
    }

    public Task<string> RunAsync(InvocationContext ctx)
    {
        var templatePackageVersion = Tools.GetNextVersion(new NuGetRestoreSettings("Pure.DI.Templates"), new NuGetVersion(2, 0, 0));
        var props = new[]
        {
            ("configuration", _settings.Configuration),
            ("version", templatePackageVersion.ToString()!)
        };

        var projectDirectory = Path.Combine("src", "Pure.DI.Templates");
        var pack = new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.Templates.csproj"))
            .WithProps(props);

        Assertion.Succeed(pack.Build());
        
        var targetPackage = Path.Combine(projectDirectory, "bin", $"Pure.DI.Templates.{templatePackageVersion}.nupkg");
        _teamCityWriter.PublishArtifact($"{targetPackage} => .");

        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(targetPackage)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(_settings.NuGetKey);

            Assertion.Succeed(push.Build());
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
        }

        return Task.FromResult(targetPackage);
    }
}