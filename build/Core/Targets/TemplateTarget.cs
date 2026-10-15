// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

using System.IO.Compression;
using NuGet.Versioning;

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
        var maxPackageVersionStr = GetMaxPackageVersion(packageVersion).ToString();
        var templatesPath = Path.Combine("src", ProjectName, "Templates");

        string[] templateProjects = [
            Path.Combine(templatesPath, "Pure.DI.Template.ClassLibrary"),
            Path.Combine(templatesPath, "Pure.DI.Template.ConsoleApp")
        ];
        EnsureAgentGuidanceIsCurrent(templateProjects);

        var jsonConfigPath = Path.Combine(".template.config", "template.json");
        var jsonConfigs = templateProjects.Select(templateProjectPath => Path.Combine(templateProjectPath, jsonConfigPath)).ToList();

        var originalJsonConfigs = await UpdateTemplateJsonFilesAsync(
            jsonConfigs,
            packageVersionStr,
            maxPackageVersionStr,
            cancellationToken);

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
            EnsureTemplatePackageHasResolvedVersions(targetPackage);
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
            await RestoreJsonFilesAsync(originalJsonConfigs, cancellationToken);
        }
    }

    private static void EnsureAgentGuidanceIsCurrent(IEnumerable<string> templateProjects)
    {
        var contextRoot = Path.GetFullPath("agent-context");
        var sourceFiles = Directory.EnumerateFiles(contextRoot, "*", SearchOption.AllDirectories).ToArray();
        foreach (var template in templateProjects)
        foreach (var source in sourceFiles)
        {
            var relative = Path.GetRelativePath(contextRoot, source);
            var targets = relative.StartsWith($"skills{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                ? new[]
                {
                    Path.Combine(template, ".agents", relative),
                    Path.Combine(template, ".claude", relative)
                }
                : new[] { Path.Combine(template, relative) };
            foreach (var target in targets)
            {
                if (File.Exists(target) && File.ReadAllBytes(source).AsSpan().SequenceEqual(File.ReadAllBytes(target))) continue;
                throw new InvalidOperationException($"Agent guidance is out of sync: {target}. Copy it from {source}.");
            }
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
            await CreateAndBuildTemplateProjectAsync(
                "di",
                "SmokeConsoleCustomComposition",
                Path.Combine(outputDirectory, "SmokeConsoleCustomComposition"),
                hiveDirectory,
                cancellationToken,
                "CustomComposition");
            await CreateAndBuildTemplateProjectAsync(
                "dilib",
                "SmokeLibCustomComposition",
                Path.Combine(outputDirectory, "SmokeLibCustomComposition"),
                hiveDirectory,
                cancellationToken,
                "CustomComposition");
            await CreateAndAssertBuildFailsAsync(
                "di",
                "SmokeConsoleBadComposition",
                Path.Combine(outputDirectory, "SmokeConsoleBadComposition"),
                hiveDirectory,
                "123Bad",
                cancellationToken);
            await CreateAndAssertBuildFailsAsync(
                "di",
                "SmokeConsoleRootComposition",
                Path.Combine(outputDirectory, "SmokeConsoleRootComposition"),
                hiveDirectory,
                "Root",
                cancellationToken);
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
        CancellationToken cancellationToken,
        string? compositionName = null)
    {
        var dotNetNew = new DotNetNew()
            .WithTemplateName(templateName)
            .WithName(projectName)
            .WithOutput(outputDirectory)
            .WithForce(true)
            .AddArgs("--debug:custom-hive", hiveDirectory)
            .WithShortName($"creating a project from the {templateName} template");

        if (!string.IsNullOrWhiteSpace(compositionName))
        {
            dotNetNew = dotNetNew.AddArgs("--compositionName", compositionName);
        }

        await dotNetNew.RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        foreach (var relativePath in new[]
                 {
                     "AGENTS.md",
                     "CLAUDE.md",
                     Path.Combine(".agents", "skills", "pure-di", "SKILL.md"),
                     Path.Combine(".claude", "skills", "pure-di", "SKILL.md")
                 })
        {
            var guidancePath = Path.Combine(outputDirectory, relativePath);
            if (File.Exists(guidancePath)) continue;
            Error($"The generated project is missing agent guidance: {guidancePath}");
            throw new InvalidOperationException($"The generated project is missing agent guidance: {guidancePath}");
        }

        await new DotNetBuild()
            .WithProject(Path.Combine(outputDirectory, $"{projectName}.csproj"))
            .WithShortName($"building the project from the {templateName} template")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
    }

    private static async Task CreateAndAssertBuildFailsAsync(
        string templateName,
        string projectName,
        string outputDirectory,
        string hiveDirectory,
        string compositionName,
        CancellationToken cancellationToken)
    {
        await new DotNetNew()
            .WithTemplateName(templateName)
            .WithName(projectName)
            .WithOutput(outputDirectory)
            .WithForce(true)
            .AddArgs("--debug:custom-hive", hiveDirectory, "--compositionName", compositionName)
            .WithShortName($"creating a project from the {templateName} template with invalid composition name {compositionName}")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var result = await new DotNetBuild()
            .WithProject(Path.Combine(outputDirectory, $"{projectName}.csproj"))
            .WithShortName($"building the project from the {templateName} template with invalid composition name {compositionName}")
            .RunAsync(cancellationToken: cancellationToken);

        if (result.ExitCode == 0)
        {
            Error($"The {templateName} template unexpectedly accepted the composition name '{compositionName}'.");
            throw new InvalidOperationException($"The {templateName} template unexpectedly accepted the composition name '{compositionName}'.");
        }
    }

    private static NuGetVersion GetMaxPackageVersion(NuGetVersion packageVersion) =>
        new(packageVersion.Major, packageVersion.Minor + 1, 0);

    private static bool HasUnresolvedVersionDefaults(string content) =>
        content.Contains("$(PureDIVersionDefault)", StringComparison.Ordinal)
        || content.Contains("$(PureDIMaxVersionDefault)", StringComparison.Ordinal);

    private static void EnsureTemplatePackageHasResolvedVersions(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var entry in archive.Entries.Where(i => i.FullName.EndsWith("template.json", StringComparison.Ordinal)))
        {
            using var streamReader = new StreamReader(entry.Open());
            var content = streamReader.ReadToEnd();
            if (!HasUnresolvedVersionDefaults(content))
            {
                continue;
            }

            Error($"The template package contains unresolved Pure.DI version placeholders: {entry.FullName}");
            throw new InvalidOperationException($"The template package contains unresolved Pure.DI version placeholders: {entry.FullName}");
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> UpdateTemplateJsonFilesAsync(
        IEnumerable<string> jsonFiles,
        string packageVersionStr,
        string maxPackageVersionStr,
        CancellationToken cancellationToken)
    {
        var originalJsonConfigs = new Dictionary<string, string>();
        foreach (var jsonFile in jsonFiles)
        {
            var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            originalJsonConfigs.Add(jsonFile, content);

            content = content
                .Replace("$(PureDIVersionDefault)", packageVersionStr)
                .Replace("$(PureDIMaxVersionDefault)", maxPackageVersionStr);

            if (HasUnresolvedVersionDefaults(content))
            {
                Error($"Could not resolve Pure.DI version placeholders in {jsonFile}");
                throw new InvalidOperationException($"Could not resolve Pure.DI version placeholders in {jsonFile}");
            }

            await File.WriteAllTextAsync(jsonFile, content, cancellationToken);
        }

        return originalJsonConfigs;
    }

    private static async Task RestoreJsonFilesAsync(
        IReadOnlyDictionary<string, string> jsonFiles,
        CancellationToken cancellationToken)
    {
        foreach (var (jsonFile, content) in jsonFiles)
        {
            await File.WriteAllTextAsync(jsonFile, content, cancellationToken);
        }
    }
}
