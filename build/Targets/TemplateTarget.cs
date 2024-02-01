// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Targets;

internal class TemplateTarget(
    Settings settings,
    ICommands commands,
    IVersions versions,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<string>
{
    public ValueTask InitializeAsync() => commands.Register(
        this,
        "Creates and deploys templates",
        "template",
        "dt");
    
    public ValueTask<string> RunAsync(CancellationToken cancellationToken)
    {
        Info("Creating templates");
        var templatePackageVersion = settings.VersionOverride ?? versions.GetNext(new NuGetRestoreSettings("Pure.DI.Templates"), settings.VersionRange);
        var props = new[]
        {
            ("configuration", settings.Configuration),
            ("version", templatePackageVersion.ToString()!)
        };

        var projectDirectory = Path.Combine("src", "Pure.DI.Templates");
        var pack = new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.Templates.csproj"))
            .WithProps(props);

        pack.Build().Succeed();
        
        var targetPackage = Path.Combine(projectDirectory, "bin", $"Pure.DI.Templates.{templatePackageVersion}.nupkg");
        artifactsWriter.PublishArtifact($"{targetPackage} => .");

        if (!string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            var push = new DotNetNuGetPush()
                .WithPackage(targetPackage)
                .WithSources("https://api.nuget.org/v3/index.json")
                .WithApiKey(settings.NuGetKey);

            push.Build().Succeed();
        }
        else
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
        }

        return ValueTask.FromResult(targetPackage);
    }
}