// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf
namespace Build.Targets;

using System.IO.Compression;
using NuGet.Versioning;

internal class TestTarget(
    Settings settings,
    ICommands commands,
    IVersions versions,
    ISdk sdk)
    : IInitializable, ITarget<BuildResult>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Builds and tests packages",
        "test",
        "t");
    
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<BuildResult> RunAsync(CancellationToken cancellationToken)
    {
        Info("Building");
        var packages = new List<string>();
        
        // Generator package
        var generatorPackageVersion = settings.VersionOverride ?? versions.GetNext(new NuGetRestoreSettings("Pure.DI"), settings.VersionRange);
        var generatorProjectDirectory = Path.Combine("src", "Pure.DI");
        var generatorPackages = settings.CodeAnalysis
            .Select(codeAnalysis => CreateGeneratorPackage(generatorPackageVersion, codeAnalysis, generatorProjectDirectory));

        var generatorPackage = Path.GetFullPath(MergeGeneratorPackages(generatorPackageVersion, generatorPackages, generatorProjectDirectory));
        packages.Add(generatorPackage);
        
        // Libraries
        Library[] libraries =
        [
            new Library(
                "Pure.DI.MS",
                GetPackagePath("Pure.DI.MS", generatorPackageVersion),
                sdk.Versions
                    .Where(i => i.Version.Major >= 7)
                    .Select(v => $"net{v.Version.Major}.{v.Version.Minor}")
                    .ToArray(),
                ["webapi"])
        ];

        foreach (var library in libraries)
        {
            var props = new[]
            {
                ("configuration", settings.Configuration),
                ("version", generatorPackageVersion.ToString())
            };

            var libraryPackResult = await new DotNetPack()
                .WithProps(props)
                .WithConfiguration(settings.Configuration)
                .WithNoBuild(true)
                .WithNoLogo(true)
                .WithProject(Path.Combine(Path.GetFullPath(Path.Combine("src", library.Name)), $"{library.Name}.csproj"))
                .BuildAsync(cancellationToken: cancellationToken);

            libraryPackResult.Succeed();
        }

        return new BuildResult(
            packages,
            generatorPackageVersion,
            generatorPackage,
            libraries);
    }

    private string GetPackagePath(string library, NuGetVersion version)
    {
        var libraryProjectDirectory = Path.GetFullPath(Path.Combine("src", library));
        var libraryPackageDir = Path.Combine(libraryProjectDirectory, "bin", settings.Configuration);
        var libraryPackageName = $"{library}.{version.ToString()}.nupkg";
        return Path.Combine(libraryPackageDir, libraryPackageName);
    }

    private string CreateGeneratorPackage(NuGetVersion packageVersion, CodeAnalysis codeAnalysis, string projectDirectory)
    {
        var analyzerRoslynPackageVersion = codeAnalysis.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        Info($"Build package for Roslyn {analyzerRoslynVersion}.");

        List<(string, string)> props = [
            ("configuration", settings.Configuration),
            ("version", packageVersion.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString())
        ];

        if (settings.BuildServer)
        {
            props.Add(("CI", "true"));
        }
        
        Info($"Building {codeAnalysis.AnalyzerRoslynPackageVersion}");
        var build = new MSBuild()
            .WithTarget("clean;rebuild")
            .WithRestore(true)
            .WithProps(props);
        
        var buildResult = build.Build();
        buildResult.Succeed();

        Info($"Testing {codeAnalysis.AnalyzerRoslynPackageVersion}");
        var test = new DotNetTest()
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true);

        var testResult = test.Build();
        WriteLine(testResult.ToString(), Color.Details);
        testResult.Succeed();

        Info($"Packing {codeAnalysis.AnalyzerRoslynPackageVersion}");
        var pack = new DotNetPack()
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"));
        
        pack.Build().Succeed();
        return Path.Combine(projectDirectory, "bin", $"roslyn{analyzerRoslynVersion}", settings.Configuration, $"Pure.DI.{packageVersion.ToString()}.nupkg");
    }
    
    private string MergeGeneratorPackages(NuGetVersion packageVersion, IEnumerable<string> mergingPackages, string projectDirectory)
    {
        var targetPackage = Path.GetFullPath(Path.Combine(projectDirectory, "bin", settings.Configuration, $"Pure.DI.{packageVersion}.nupkg"));
        Info($"Creating NuGet package {targetPackage}");
        var targetDir = Path.GetDirectoryName(targetPackage);
        if (!string.IsNullOrWhiteSpace(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (File.Exists(targetPackage))
        {
            File.Delete(targetPackage);
        }

        using var outStream = File.Create(targetPackage);
        using var outArchive = new ZipArchive(outStream, ZipArchiveMode.Create);
        var buffer = new byte[4096];
        var paths = new HashSet<string>();
        foreach (var package in mergingPackages)
        {
            Info($"Processing \"{package}\".");
            using var inStream = File.OpenRead(package);
            using var inArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
            foreach (var entry in inArchive.Entries)
            {
                if (entry.Length <= 0 || !paths.Add(entry.FullName))
                {
                    WriteLine($"{entry.FullName,-100} - skipped", Color.Details);
                    continue;
                }

                using var prevStream = entry.Open();
                var newEntry = outArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var newStream = newEntry.Open();
                int size;
                do
                {
                    size = prevStream.Read(buffer, 0, buffer.Length);
                    if (size > 0)
                    {
                        newStream.Write(buffer, 0, size);
                    }
                } while (size > 0);
                newStream.Flush();
                WriteLine($"{entry.FullName,-100} - merged", Color.Details);
            }
        }
        
        return targetPackage;
    }
}