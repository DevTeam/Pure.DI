// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Targets;

internal class TemplateTarget(
    Settings settings,
    ICommands commands,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<string>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Creates and deploys templates",
        "template",
        "dt");
    
    public Task<string> RunAsync(CancellationToken cancellationToken)
    {
        Info("Creating templates");
        var props = new[]
        {
            ("configuration", settings.Configuration),
            ("version", settings.Version.ToString()!)
        };

        var projectDirectory = Path.Combine("src", "Pure.DI.Templates");
        var pack = new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.Templates.csproj"))
            .WithProps(props);

        pack.Build().Succeed();
        
        var targetPackage = Path.Combine(projectDirectory, "bin", $"Pure.DI.Templates.{settings.Version}.nupkg");
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