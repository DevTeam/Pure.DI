namespace Build.Core.Targets;

using System.Runtime.CompilerServices;

class GeneratorTarget(
    Settings settings,
    Commands commands,
    Packages packages,
    Env env)
    : IInitializable, ITarget<Package>
{
    // The Roslyn API levels used by the '#if ROSLYN*_OR_GREATER' directives of the sources and tests.
    // A package defines every level its Roslyn version supports, so a newer compiler also compiles the code
    // of the older levels. Add a level here when a gate has to tell it apart from a lower one.
    private static readonly Version[] RoslynApiLevels =
    [
        new(5, 9),
        new(4, 8),
        new(4, 3)
    ];

    private string PackageName => $"Pure.DI.{settings.NextVersion}.nupkg";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Build and test the source code generator", "generator", "g");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var generatorProjectDirectory = Path.Combine("src", "Pure.DI");
        var mergedPackagePath = Path.GetFullPath(Path.Combine(generatorProjectDirectory, env.GetPath(PathType.PackagesDirectory), PackageName));
        var generatorPackages = CreateGeneratorPackagesAsync(generatorProjectDirectory, cancellationToken);
        var mergedPackage = await packages.MergeAsync(generatorPackages, mergedPackagePath, cancellationToken);
        return new Package(mergedPackage, true, settings.NextVersion);
    }

    private async IAsyncEnumerable<string> CreateGeneratorPackagesAsync(string generatorProjectDirectory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var selection = GetRoslynSelection();
        foreach (var codeAnalysis in settings.CodeAnalysis)
        {
            if (selection.Build(codeAnalysis))
            {
                yield return await CreateGeneratorPackageAsync(codeAnalysis, generatorProjectDirectory, cancellationToken);
                continue;
            }

            if (selection.Prebuilt)
            {
                yield return GetPrebuiltGeneratorPackage(codeAnalysis);
                continue;
            }

            Warning($"The package for Roslyn {GetRoslynVersion(codeAnalysis)} is skipped, the merged package {PackageName} is incomplete.");
        }
    }

    // The "roslyn" property lets CI build and test each Roslyn version in its own job and then merge the packages these jobs created.
    private RoslynSelection GetRoslynSelection()
    {
        var roslyn = settings.Roslyn.Trim();
        if (roslyn.Length == 0)
        {
            return new RoslynSelection(_ => true, false);
        }

        if (string.Equals(roslyn, "prebuilt", StringComparison.OrdinalIgnoreCase))
        {
            return new RoslynSelection(_ => false, true);
        }

        var supported = settings.CodeAnalysis.Select(GetRoslynVersion).ToList();
        var versions = roslyn
            .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(i => i switch
            {
                _ when string.Equals(i, "newest", StringComparison.OrdinalIgnoreCase) => supported.Max()!,
                _ when Version.TryParse(i, out var version) => new Version(version.Major, version.Minor),
                _ => throw new ArgumentException($"Invalid Roslyn version: '{i}'.")
            })
            .ToHashSet();

        var unknown = versions.Except(supported).ToList();
        return unknown.Count > 0
            ? throw new ArgumentException($"Unknown Roslyn versions: {string.Join(", ", unknown)}. Supported versions: {string.Join(", ", supported)}.")
            : new RoslynSelection(codeAnalysis => versions.Contains(GetRoslynVersion(codeAnalysis)), false);
    }

    private string GetPrebuiltGeneratorPackage(CodeAnalysis codeAnalysis)
    {
        var roslynVersion = GetRoslynVersion(codeAnalysis);
        var packagePath = Path.Combine(env.GetPath(PathType.PackagesDirectory), roslynVersion.ToString(), PackageName);
        if (!File.Exists(packagePath))
        {
            throw new InvalidOperationException($"The prebuilt package for Roslyn {roslynVersion} was not found: {packagePath}");
        }

        Summary("Use prebuilt package for Roslyn ", roslynVersion.WithColor(Color.Details));
        return packagePath;
    }

    private static Version GetRoslynVersion(CodeAnalysis codeAnalysis) =>
        new(codeAnalysis.AnalyzerRoslynPackageVersion.Major, codeAnalysis.AnalyzerRoslynPackageVersion.Minor);

    private record RoslynSelection(Func<CodeAnalysis, bool> Build, bool Prebuilt);

    private async Task<string> CreateGeneratorPackageAsync(CodeAnalysis codeAnalysis, string projectDirectory, CancellationToken cancellationToken)
    {
        var analyzerRoslynPackageVersion = codeAnalysis.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        Summary("Build package for Roslyn ", analyzerRoslynVersion.WithColor(Color.Details));

        var roslynVersions = string.Join(
            ';',
            RoslynApiLevels
                .Where(level => level <= analyzerRoslynVersion)
                .Select(level => $"ROSLYN{level.Major}_{level.Minor}_OR_GREATER"));

        List<(string, string)> props =
        [
            ("configuration", settings.Configuration),
            ("version", settings.NextVersion.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString()),
            ("RoslynVersions", $"\"{roslynVersions}\"")
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

        await new DotNetRestore()
            .WithShortName($"restoring {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        props.Add(("PackGenerator", "true"));

        await new DotNetBuild()
            .WithShortName($"building {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithNoRestore(true)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        if (settings.Tests)
        {
            var test = new DotNetTest()
                .WithShortName($"testing {codeAnalysis.AnalyzerRoslynPackageVersion}")
                .WithProps(props)
                .WithConfiguration(settings.Configuration)
                .WithNoBuild(true).WithNoLogo(true);

            // The size of the generated code does not depend on the Roslyn version, so these slow tests run only with the newest one.
            // CI runs them in a separate job: "codeSize" is "skip" in the regular jobs and "only" in that job.
            var isNewest = codeAnalysis.AnalyzerRoslynPackageVersion == settings.CodeAnalysis.Max(i => i.AnalyzerRoslynPackageVersion);
            var codeSize = settings.CodeSize.Trim().ToLowerInvariant();
            var filter = codeSize switch
            {
                "" => isNewest ? null : "Category!=CodeSize",
                "skip" => "Category!=CodeSize",
                "only" => "Category=CodeSize",
                _ => throw new ArgumentException($"Invalid codeSize value: '{settings.CodeSize}'. Use \"skip\" or \"only\".")
            };

            if (filter is not null)
            {
                test = test.WithFilter(filter);
            }

            await test.BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }

        var packagePath = Path.Combine(env.GetPath(PathType.PackagesDirectory), analyzerRoslynVersion.ToString());

        await new DotNetPack()
            .WithShortName($"packing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"))
            .WithOutput(packagePath)
            .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();

        return Path.Combine(packagePath, PackageName);
    }
}