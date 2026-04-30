// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class TemplateTarget(
    Settings settings,
    Commands commands,
    Env env,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<Package>
{
    public const string ProjectName = "Pure.DI.Templates";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Create and deploy templates", "template", "t");

    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var packageVersion = settings.CurrentVersion;
        var packageVersionStr = packageVersion.ToString();
        var templatesPath = Path.Combine("src", ProjectName, "Templates");

        string[] templateProjects = [
            Path.Combine(templatesPath, "Pure.DI.Template.ClassLibrary"),
            Path.Combine(templatesPath, "Pure.DI.Template.ConsoleApp")
        ];

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
            await SmokeTestTemplatePackageAsync(targetPackage, cancellationToken);

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

    private async Task SmokeTestTemplatePackageAsync(string packagePath, CancellationToken cancellationToken)
    {
        Summary("Smoke testing template package ", packagePath.WithColor(Color.Details));

        var tempDirectory = env.GetPath(PathType.TempDirectory);
        var hiveDirectory = Path.Combine(tempDirectory, "hive");
        var outputDirectory = Path.Combine(tempDirectory, "out");
        var consoleAppDirectory = Path.Combine(outputDirectory, "SmokeConsole");
        var classLibraryDirectory = Path.Combine(outputDirectory, "SmokeLib");

        try
        {
            Directory.CreateDirectory(hiveDirectory);
            Directory.CreateDirectory(outputDirectory);

            await new DotNetNewInstall()
                .WithPackage(Path.GetFullPath(packagePath))
                .AddArgs("--debug:custom-hive", hiveDirectory)
                .WithShortName("installing the template package into a custom hive")
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            await CreateAndBuildTemplateProjectAsync("di", "SmokeConsole", consoleAppDirectory, hiveDirectory, cancellationToken);
            await CreateAndBuildTemplateProjectAsync("dilib", "SmokeLib", classLibraryDirectory, hiveDirectory, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private static async Task CreateAndBuildTemplateProjectAsync(
        string templateName,
        string projectName,
        string outputDirectory,
        string hiveDirectory,
        CancellationToken cancellationToken)
    {
        await new DotNetNew()
            .WithTemplateName(templateName)
            .WithName(projectName)
            .WithOutput(outputDirectory)
            .WithForce(true)
            .AddArgs("--debug:custom-hive", hiveDirectory)
            .WithShortName($"creating a project from the {templateName} template")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var guidelinesPath = Path.Combine(outputDirectory, ".junie", "guidelines.md");
        if (File.Exists(guidelinesPath))
        {
            Error($"The generated project contains unexpected file: {guidelinesPath}");
            throw new InvalidOperationException($"The generated project contains unexpected file: {guidelinesPath}");
        }

        await new DotNetBuild()
            .WithProject(Path.Combine(outputDirectory, $"{projectName}.csproj"))
            .WithShortName($"building the project from the {templateName} template")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
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
