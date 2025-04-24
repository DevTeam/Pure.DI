// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class TemplateTarget(
    Settings settings,
    Commands commands,
    Versions versions,
    Env env,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<Package>
{
    public const string ProjectName = "Pure.DI.Templates";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Create and deploy templates", "template", "t");

    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var packageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), Settings.VersionRange, 0);
        var packageVersionStr = packageVersion.ToString();
        var templatesPath = Path.Combine("src", ProjectName, "Templates");
        var jsonConfigPath = Path.Combine(".template.config", "template.json");
        var jsonConfigs = new[]
        {
            Path.Combine(templatesPath, "Pure.DI.Template.ClassLibrary", jsonConfigPath),
            Path.Combine(templatesPath, "Pure.DI.Template.ConsoleApp", jsonConfigPath)
        };

        foreach (var jsonFile in jsonConfigs)
        {
            var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            content = content.Replace("$(version)", packageVersionStr);
            await File.WriteAllTextAsync(jsonFile, content, cancellationToken);
        }

        var props = new[]
        {
            ("configuration", settings.Configuration),
            ("version", packageVersionStr)
        };

        var projectDirectory = Path.Combine("src", ProjectName);
        await new DotNetPack()
            .WithProject(Path.Combine(projectDirectory, $"{ProjectName}.csproj"))
            .WithProps(props)
            .WithOutput(env.GetPath(PathType.PackagesDirectory))
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var targetPackage = Path.Combine(env.GetPath(PathType.PackagesDirectory), "bin", $"{ProjectName}.{packageVersion}.nupkg");
        artifactsWriter.PublishArtifact($"{targetPackage} => .");

        if (string.IsNullOrWhiteSpace(settings.NuGetKey))
        {
            Warning($"The NuGet key was not specified, the package {targetPackage} will not be pushed.");
            return new Package(targetPackage, false, packageVersion);
        }

        await new DotNetNuGetPush()
            .WithPackage(targetPackage)
            .WithSource("https://api.nuget.org/v3/index.json")
            .WithApiKey(settings.NuGetKey)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        return new Package(targetPackage, true, packageVersion);
    }
}