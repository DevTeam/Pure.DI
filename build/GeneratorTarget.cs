namespace Build;

using System.Runtime.CompilerServices;

internal class GeneratorTarget(
    Settings settings,
    Commands commands,
    Packages packages)
    : IInitializable, ITarget<Package>
{
    private const string PackagesDir = ".packages";

    private string PackageName => $"Pure.DI.{settings.NextVersion}.nupkg";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Build and test the source code generator", "generator", "g");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var generatorProjectDirectory = Path.Combine("src", "Pure.DI");
        var mergedPackagePath = Path.GetFullPath(Path.Combine(generatorProjectDirectory, PackagesDir, PackageName));
        var generatorPackages = CreateGeneratorPackagesAsync(generatorProjectDirectory, cancellationToken);
        var mergedPackage = await packages.MergeAsync(generatorPackages, mergedPackagePath, cancellationToken);
        return new Package(mergedPackage, true, settings.NextVersion);
    }

    private async IAsyncEnumerable<string> CreateGeneratorPackagesAsync(string generatorProjectDirectory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var codeAnalysis in settings.CodeAnalysis)
        {
            yield return await CreateGeneratorPackageAsync(codeAnalysis, generatorProjectDirectory, cancellationToken);
        }
    }

    private async Task<string> CreateGeneratorPackageAsync(CodeAnalysis codeAnalysis, string projectDirectory, CancellationToken cancellationToken)
    {
        var analyzerRoslynPackageVersion = codeAnalysis.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        Info($"Build package for Roslyn {analyzerRoslynVersion}.");

        var rolsynVersions = string.Join(
            ';',
            settings.CodeAnalysis
                .Where(i => i.AnalyzerRoslynPackageVersion <= codeAnalysis.AnalyzerRoslynPackageVersion)
                .Select(i => $"ROSLYN{i.AnalyzerRoslynPackageVersion.Major}_{i.AnalyzerRoslynPackageVersion.Minor}_OR_GREATER"));

        List<(string, string)> props =
        [
            ("configuration", settings.Configuration),
            ("version", settings.NextVersion.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString()),
            ("RolsynVersions", $"\"{rolsynVersions}\"")
        ];

        if (settings.BuildServer)
        {
            props.Add(("CI", "true"));
        }

        var bin = Path.Combine(projectDirectory, "bin");
        if (Directory.Exists(bin))
        {
            Directory.Delete(bin, true);
        }

        var obj = Path.Combine(projectDirectory, "obj");
        if (Directory.Exists(obj))
        {
            Directory.Delete(obj, true);
        }

        await new DotNetBuild()
            .WithShortName($"building {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        await new DotNetTest()
            .WithShortName($"testing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true).WithNoLogo(true)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var packagePath = Path.Combine(PackagesDir, analyzerRoslynVersion.ToString());

        await new DotNetPack()
            .WithShortName($"packing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"))
            .WithOutput(Path.Combine(projectDirectory, packagePath))
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        return Path.Combine(projectDirectory, packagePath, PackageName);
    }
}