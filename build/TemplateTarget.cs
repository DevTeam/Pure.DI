// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;

internal class TemplateTarget: Command, ITarget<string>
{
    private readonly Settings _settings;
    private readonly ITeamCityWriter _teamCityWriter;

    public TemplateTarget(
        Settings settings,
        ITeamCityWriter teamCityWriter)
        : base("template", "Push NuGet packages")
    {
        _settings = settings;
        _teamCityWriter = teamCityWriter;
        this.SetHandler(RunAsync);
        AddAlias("t");
    }
    
    public Task<string> RunAsync(InvocationContext ctx)
    {
        var templatePackageVersion = _settings.VersionOverride ?? new NuGetRestoreSettings("Pure.DI.Templates").GetNextVersion(_settings.VersionRange);
        var props = new[]
        {
            ("configuration", _settings.Configuration),
            ("version", templatePackageVersion.ToString()!)
        };

        var projectDirectory = Path.Combine("src", "Pure.DI.Templates");
        var pack = new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.Templates.csproj"))
            .WithProps(props);

        pack.Build().Succeed();
        
        var targetPackage = Path.Combine(projectDirectory, "bin", $"Pure.DI.Templates.{templatePackageVersion}.nupkg");
        _teamCityWriter.PublishArtifact($"{targetPackage} => .");

        if (!string.IsNullOrWhiteSpace(_settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(targetPackage)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(_settings.NuGetKey);

            push.Build().Succeed();
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
        }

        return Task.FromResult(targetPackage);
    }
}