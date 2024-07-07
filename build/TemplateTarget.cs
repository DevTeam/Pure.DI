// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

internal class TemplateTarget(
    Settings settings,
    Commands commands,
    Versions versions,
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
        var packageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0).ToString();
        foreach (var jsonFile in new[]
                 {
                     "src/Pure.DI.Templates/Templates/Pure.DI.Template.ClassLibrary/.template.config/template.json",
                     "src/Pure.DI.Templates/Templates/Pure.DI.Template.ConsoleApp/.template.config/template.json"
                 })
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
        new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, $"{ProjectName}.csproj"))
            .WithProps(props)
            .Build()
            .EnsureSuccess();

        var targetPackage = Path.Combine(projectDirectory, "bin", $"{ProjectName}.{packageVersion}.nupkg");
        artifactsWriter.PublishArtifact($"{targetPackage} => .");

        if (string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
            return targetPackage;
        }

        new DotNetNuGetPush()
            .WithPackage(targetPackage)
            .WithSources("https://api.nuget.org/v3/index.json")
            .WithApiKey(settings.NuGetKey)
            .Build()
            .EnsureSuccess();

        return targetPackage;
    }
}