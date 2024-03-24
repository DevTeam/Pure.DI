// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build;

using System.IO.Compression;

internal class GeneratorTarget(
    Settings settings,
    Commands commands)
    : IInitializable, ITarget<Package>
{
    private const string PackagesDir = ".packages";
    
    private string PackageName => $"Pure.DI.{settings.Version}.nupkg";

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

        return Task.FromResult(
            new Package(
                Path.GetFullPath(MergeGeneratorPackages(generatorPackages, generatorProjectDirectory)),
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
            ("version", settings.Version.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString()),
            ("RolsynVersions", $"\"{rolsynVersions}\""),
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
            .Succeed();

        var testResult = new DotNetTest()
            .WithShortName($"Testing {codeAnalysis.AnalyzerRoslynPackageVersion}")
            .WithProps(props)
            .WithConfiguration(settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .Build();

        WriteLine(testResult.ToString(), Color.Details);
        testResult.Succeed();

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
            .Succeed();

        return Path.Combine(projectDirectory, packagePath, PackageName);
    }

    private string MergeGeneratorPackages(IEnumerable<string> mergingPackages, string projectDirectory)
    {
        var targetPackage = Path.GetFullPath(Path.Combine(projectDirectory, PackagesDir, PackageName));
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