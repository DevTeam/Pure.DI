// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

using System.IO.Compression;
using System.Xml.Linq;
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
        var packageVersion = string.IsNullOrWhiteSpace(settings.RequestedVersion)
            ? settings.CurrentVersion
            : settings.NextVersion;
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
            $"net{settings.BaseDotNetFrameworkVersion}",
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

    private async Task SmokeTestTemplatePackageAsync(
        string packagePath,
        CancellationToken cancellationToken)
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

            // Release smoke tests must consume the newly built generator, including prereleases,
            // without reusing a previously cached package with the same version.
            if (!string.IsNullOrWhiteSpace(settings.TemplatePackageSource))
            {
                var packageSource = Path.GetFullPath(settings.TemplatePackageSource);
                var config = new XDocument(new XElement("configuration",
                    new XElement("config", new XElement("add",
                        new XAttribute("key", "globalPackagesFolder"),
                        new XAttribute("value", Path.Combine(tempDirectory, "packages")))),
                    new XElement("packageSources", new XElement("clear"),
                        new XElement("add", new XAttribute("key", "release"), new XAttribute("value", packageSource)),
                        new XElement("add", new XAttribute("key", "nuget.org"), new XAttribute("value", "https://api.nuget.org/v3/index.json"))),
                    new XElement("packageSourceMapping", new XElement("clear"),
                        new XElement("packageSource", new XAttribute("key", "release"),
                            new XElement("package", new XAttribute("pattern", "Pure.DI"))),
                        new XElement("packageSource", new XAttribute("key", "nuget.org"),
                            new XElement("package", new XAttribute("pattern", "*"))))));
                config.Save(Path.Combine(outputDirectory, "NuGet.Config"));
            }

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
            await CreateAndBuildTemplateProjectAsync(
                "di",
                "SmokeConsoleOldFramework",
                Path.Combine(outputDirectory, "SmokeConsoleOldFramework"),
                hiveDirectory,
                cancellationToken,
                framework: "net8.0");
            await CreateAndAssertUsesExactVersionAsync(
                "SmokeConsoleExactVersion",
                Path.Combine(outputDirectory, "SmokeConsoleExactVersion"),
                hiveDirectory,
                cancellationToken);
            await CreateAndBuildTemplateProjectAsync(
                "di",
                "SmokeConsoleBadComposition",
                Path.Combine(outputDirectory, "SmokeConsoleBadComposition"),
                hiveDirectory,
                cancellationToken,
                "123Bad",
                expectBuildSuccess: false);
            await CreateAndBuildTemplateProjectAsync(
                "di",
                "SmokeConsoleRootComposition",
                Path.Combine(outputDirectory, "SmokeConsoleRootComposition"),
                hiveDirectory,
                cancellationToken,
                "Root",
                expectBuildSuccess: false);
            await PackLibraryAndBuildConsumerAsync(
                classLibraryDirectory,
                outputDirectory,
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
        string? compositionName = null,
        string? framework = null,
        bool expectBuildSuccess = true)
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

        if (!string.IsNullOrWhiteSpace(framework))
        {
            dotNetNew = dotNetNew.AddArgs("--framework", framework);
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

        var result = await new DotNetBuild()
            .WithProject(Path.Combine(outputDirectory, $"{projectName}.csproj"))
            .WithShortName($"building the project from the {templateName} template")
            .RunAsync(cancellationToken: cancellationToken);

        if (expectBuildSuccess)
        {
            result.EnsureSuccess();
            return;
        }

        if (result.ExitCode == 0)
        {
            Error($"The {templateName} template unexpectedly accepted the composition name '{compositionName}'.");
            throw new InvalidOperationException($"The {templateName} template unexpectedly accepted the composition name '{compositionName}'.");
        }
    }

    private static async Task CreateAndAssertUsesExactVersionAsync(
        string projectName,
        string outputDirectory,
        string hiveDirectory,
        CancellationToken cancellationToken)
    {
        // An explicit --version must be pinned, because the default range is anchored at the version of the
        // template package and would otherwise produce an unsatisfiable range for a version outside of it.
        await new DotNetNew()
            .WithTemplateName("di")
            .WithName(projectName)
            .WithOutput(outputDirectory)
            .WithForce(true)
            .AddArgs("--debug:custom-hive", hiveDirectory, "--version", "2.0.0")
            .WithShortName("creating a project from the di template with an explicit Pure.DI version")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var projectPath = Path.Combine(outputDirectory, $"{projectName}.csproj");
        var content = await File.ReadAllTextAsync(projectPath, cancellationToken);
        if (!content.Contains("Version=\"2.0.0\"", StringComparison.Ordinal))
        {
            Error("The di template did not pin the Pure.DI version that was passed with --version.");
            throw new InvalidOperationException("The di template did not pin the Pure.DI version that was passed with --version.");
        }
    }

    private static async Task PackLibraryAndBuildConsumerAsync(
        string classLibraryDirectory,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        // The class library template packs its composition as a source file and refers to Pure.DI with
        // PrivateAssets=all, so the packed composition is compiled by the consumer together with its own Pure.DI.
        // Without that reference the consumer fails with CS0122 on the internal Pure.DI API.
        var packagesDirectory = Path.Combine(outputDirectory, "library-packages");
        var consumerDirectory = Path.Combine(outputDirectory, "SmokeLibConsumer");
        var projectName = Path.GetFileName(classLibraryDirectory);

        await new DotNetPack()
            .WithProject(Path.Combine(classLibraryDirectory, $"{projectName}.csproj"))
            .WithOutput(packagesDirectory)
            .WithShortName("packing the project from the dilib template")
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        await new DotNetNew()
            .WithTemplateName("console")
            .WithName("SmokeLibConsumer")
            .WithOutput(consumerDirectory)
            .WithForce(true)
            .WithShortName("creating a consumer project for the dilib template package")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var consumerProject = Path.Combine(consumerDirectory, "SmokeLibConsumer.csproj");

        await new DotNetAddPackage()
            .WithProject(consumerProject)
            .WithPackage(projectName)
            .WithVersion("1.0.0")
            .AddSources(packagesDirectory)
            .WithShortName("adding the packed library to its consumer")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        // The packed composition is compiled in the consumer project, where the private Pure.DI reference of the
        // library does not apply, so the consumer refers to the same Pure.DI version itself.
        var libraryProject = Path.Combine(classLibraryDirectory, $"{projectName}.csproj");
        var libraryProjectContent = await File.ReadAllTextAsync(libraryProject, cancellationToken);
        var pureDiVersion = XDocument.Parse(libraryProjectContent)
            .Descendants("PackageReference")
            .Where(i => string.Equals((string?)i.Attribute("Include"), "Pure.DI", StringComparison.Ordinal))
            .Select(i => (string?)i.Attribute("Version"))
            .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
        if (string.IsNullOrWhiteSpace(pureDiVersion))
        {
            Error($"Could not find the Pure.DI version in {libraryProject}");
            throw new InvalidOperationException($"Could not find the Pure.DI version in {libraryProject}");
        }

        // The generated project may refer to a version range, while `dotnet add package` accepts one exact version.
        var pureDiMinVersion = (VersionRange.Parse(pureDiVersion).MinVersion
            ?? throw new InvalidOperationException($"Could not determine the Pure.DI version from '{pureDiVersion}'.")).ToString();

        await new DotNetAddPackage()
            .WithProject(consumerProject)
            .WithPackage("Pure.DI")
            .WithVersion(pureDiMinVersion)
            .WithShortName("adding Pure.DI to the consumer of the packed library")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        await new DotNetBuild()
            .WithProject(consumerProject)
            .WithShortName("building the consumer of the packed library")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
    }

    private static NuGetVersion GetMaxPackageVersion(NuGetVersion packageVersion) =>
        new(packageVersion.Major, packageVersion.Minor + 1, 0);

    private static bool HasUnresolvedVersionDefaults(string content) =>
        content.Contains("$(PureDIVersionDefault)", StringComparison.Ordinal)
        || content.Contains("$(PureDIMaxVersionDefault)", StringComparison.Ordinal)
        || content.Contains("$(PureDIFrameworkDefault)", StringComparison.Ordinal);

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

            Error($"The template package contains unresolved placeholders: {entry.FullName}");
            throw new InvalidOperationException($"The template package contains unresolved placeholders: {entry.FullName}");
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> UpdateTemplateJsonFilesAsync(
        IEnumerable<string> jsonFiles,
        string packageVersionStr,
        string maxPackageVersionStr,
        string frameworkStr,
        CancellationToken cancellationToken)
    {
        var originalJsonConfigs = new Dictionary<string, string>();
        foreach (var jsonFile in jsonFiles)
        {
            var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);
            originalJsonConfigs.Add(jsonFile, content);

            content = content
                .Replace("$(PureDIVersionDefault)", packageVersionStr)
                .Replace("$(PureDIMaxVersionDefault)", maxPackageVersionStr)
                .Replace("$(PureDIFrameworkDefault)", frameworkStr);

            if (HasUnresolvedVersionDefaults(content))
            {
                Error($"Could not resolve template placeholders in {jsonFile}");
                throw new InvalidOperationException($"Could not resolve template placeholders in {jsonFile}");
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
