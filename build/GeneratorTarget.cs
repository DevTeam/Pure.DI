namespace Build;

internal class GeneratorTarget(
    Settings settings,
    Commands commands,
    Packages packages)
    : IInitializable, ITarget<Package>
{
    private const string PackagesDir = ".packages";
    
    private string PackageName => $"Pure.DI.{settings.NextVersion}.nupkg";

    public Task InitializeAsync() => commands.Register(
        this,
        "Builds and tests generator",
        "generator",
        "g");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        // Generator package
        var generatorProjectDirectory = Path.Combine("src", "Pure.DI");
        var generatorPackages = settings.CodeAnalysis
            .Select(codeAnalysis => CreateGeneratorPackage(codeAnalysis, generatorProjectDirectory))
            .ToList();
        
        var targetPackage = Path.GetFullPath(Path.Combine(generatorProjectDirectory, PackagesDir, PackageName));
        return Task.FromResult(
            new Package(
                Path.GetFullPath(packages.Merge(generatorPackages, targetPackage)),
                true));
    }

    private string CreateGeneratorPackage(CodeAnalysis codeAnalysis, string projectDirectory)
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

        new DotNetBuild()
            .WithShortName($"Building {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .Build()
            .EnsureSuccess();

        new DotNetTest()
            .WithShortName($"Testing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .Build()
            .EnsureSuccess();

        var packagePath = Path.Combine(PackagesDir, analyzerRoslynVersion.ToString());

        new DotNetPack()
            .WithShortName($"Packing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"))
            .WithOutput(Path.Combine(projectDirectory, packagePath))
            .Build()
            .EnsureSuccess();

        return Path.Combine(projectDirectory, packagePath, PackageName);
    }
}