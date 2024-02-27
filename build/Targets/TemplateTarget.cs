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
    private const string ProjectName = "Pure.DI.Templates";
    
    public Task InitializeAsync() => commands.Register(
        this,
        "Creates and deploys templates",
        "template",
        "t");
    
    public Task<string> RunAsync(CancellationToken cancellationToken)
    {
        Info("Creating templates");
        var packageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0).ToString();
        var props = new[]
        {
            ("configuration", settings.Configuration),
            ("version", packageVersion)
        };

        var projectDirectory = Path.Combine("src", ProjectName);
        var pack = new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, $"{ProjectName}.csproj"))
            .WithProps(props);

        pack.Build().Succeed();
        
        var targetPackage = Path.Combine(projectDirectory, "bin", $"{ProjectName}.{packageVersion}.nupkg");
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

        return Task.FromResult(targetPackage);
    }
}