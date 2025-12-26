// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class TemplateTarget(
    Settings settings,
    Commands commands,
    Versions versions,
    Env env,
    ITeamCityArtifactsWriter artifactsWriter,
    [Tag(typeof(AIContextTarget))] ITarget<AIContext> aiContextTarget)
    : IInitializable, ITarget<Package>
{
    public const string ProjectName = "Pure.DI.Templates";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Create and deploy templates", "template", "t");

    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var packageVersion = versions.GetNext(new NuGetRestoreSettings("Pure.DI"), settings.VersionRange, 0);
        var packageVersionStr = packageVersion.ToString();
        var templatesPath = Path.Combine("src", ProjectName, "Templates");

        string[] templateProjects = [
            Path.Combine(templatesPath, "Pure.DI.Template.ClassLibrary"),
            Path.Combine(templatesPath, "Pure.DI.Template.ConsoleApp")
        ];

        // Update .junie/guidelines.md
        var aiContext = await aiContextTarget.RunAsync(cancellationToken);
        if (aiContext.Files.MaxBy(i => i.SizeKB) is {} aiCotext)
        {
            foreach (var templateProjectPath in templateProjects)
            {
                var juniePath = Path.Combine(templateProjectPath, ".junie");
                var guidelines = Path.Combine(juniePath, "guidelines.md");
                if (!Directory.Exists(juniePath))
                {
                    Directory.CreateDirectory(juniePath);
                }

                File.Copy(aiCotext.FileName, guidelines, true);
            }
        }
        else
        {
            Warning("The AI guidelines was not updated in templates.");
        }

        var jsonConfigPath = Path.Combine(".template.config", "template.json");
        var jsonConfigs = templateProjects.Select(templateProjectPath => Path.Combine(templateProjectPath, jsonConfigPath)).ToList();

        await UpdateJsonFileWithVersion(jsonConfigs, "$(version)", packageVersionStr, cancellationToken);

        try
        {
            var props = new[]
            {
                ("configuration", settings.Configuration),
                ("version", packageVersionStr)
            };

            var projectDirectory = Path.Combine("src", ProjectName);
            var packagesDirectory = env.GetPath(PathType.PackagesDirectory);
            await new DotNetPack()
                .WithProject(Path.Combine(projectDirectory, $"{ProjectName}.csproj"))
                .WithProps(props)
                .WithOutput(packagesDirectory)
                .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

            var targetPackage = Path.Combine(packagesDirectory, $"{ProjectName}.{packageVersion}.nupkg");
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
        finally
        {
            await UpdateJsonFileWithVersion(jsonConfigs, packageVersionStr, "$(version)", cancellationToken);
        }
    }

    private static async Task UpdateJsonFileWithVersion(
        IEnumerable<string> jsonFiles,
        string targetStr,
        string packageVersionStr, CancellationToken cancellationToken)
    {
        foreach (var jsonFile in jsonFiles)
        {
            var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            content = content.Replace(targetStr, packageVersionStr);
            await File.WriteAllTextAsync(jsonFile, content, cancellationToken);
        }
    }
}