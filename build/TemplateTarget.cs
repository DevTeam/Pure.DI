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

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Creates and deploys templates", "template", "t");

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
        await new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, $"{ProjectName}.csproj"))
            .WithProps(props)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var targetPackage = Path.Combine(projectDirectory, "bin", $"{ProjectName}.{packageVersion}.nupkg");
        artifactsWriter.PublishArtifact($"{targetPackage} => .");

        if (string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
            return targetPackage;
        }

        await new DotNetNuGetPush()
            .WithPackage(targetPackage)
            .WithSources("https://api.nuget.org/v3/index.json")
            .WithApiKey(settings.NuGetKey)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        return targetPackage;
    }
}