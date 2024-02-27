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
    
    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        Info("Creating templates");
        var packageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0).ToString();
        foreach (var jsonFile in new [] {
                     "src/Pure.DI.Templates/Templates/Pure.DI.Template.ClassLibrary/.template.config/template.json",
                     "src/Pure.DI.Templates/Templates/Pure.DI.Template.ConsoleApp/.template.config/template.json"})
        {
            var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            content = content.Replace("$(version)", packageVersion);
            await File.WriteAllTextAsync(jsonFile, content, cancellationToken);
        }
        
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

        return targetPackage;
    }
}