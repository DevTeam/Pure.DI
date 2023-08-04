// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;

internal class PackTarget: ITarget<string>, ICommandProvider
{
    private readonly Settings _settings;
    private readonly ITeamCityWriter _teamCityWriter;

    public PackTarget(
        Settings settings,
        ITeamCityWriter teamCityWriter)
    {
        _settings = settings;
        _teamCityWriter = teamCityWriter;
        Command = new Command("pack", "Creates NuGet packages");
        Command.SetHandler(RunAsync);
        Command.AddAlias("p");
    }
    
    public Command Command { get; }

    public Task<string> RunAsync(InvocationContext ctx)
    {
        var packageVersion = _settings.VersionOverride ?? Tools.GetNextVersion(new NuGetRestoreSettings("Pure.DI"), _settings.VersionRange);
        var projectDirectory = Path.Combine("src", "Pure.DI");
        var packages = _settings.CodeAnalysis
            .Select(codeAnalysis => CreatePackage(packageVersion, codeAnalysis, projectDirectory))
            .ToArray();

        var targetPackage = Path.GetFullPath(
            Path.Combine(projectDirectory, "bin", _settings.Configuration, $"Pure.DI.{packageVersion}.nupkg"));

        MergeNuGetPackages(packages, targetPackage);
        _teamCityWriter.PublishArtifact($"{targetPackage} => .");
        return Task.FromResult(targetPackage);
    }
    
    private string CreatePackage(NuGetVersion packageVersion, CodeAnalysis codeAnalysis, string projectDirectory)
    {
        var analyzerRoslynPackageVersion = codeAnalysis.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        WriteLine($"Build package for Roslyn {analyzerRoslynVersion}.", Color.Highlighted);

        var props = new[]
        {
            ("version", packageVersion.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString())
        };
        
        var build = new DotNetBuild()
            .WithProps(props)
            .WithConfiguration(_settings.Configuration);
        
        var buildResult = build.Build();
        Assertion.Succeed(buildResult);

        var test = new DotNetTest()
            .WithProps(props)
            .WithConfiguration(_settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true);

        var testResult = test.Build();
        WriteLine(testResult.ToString(), Color.Highlighted);
        Assertion.Succeed(testResult);

        var pack = new DotNetPack()
            .WithProps(props)
            .WithConfiguration(_settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"));
        
        Assertion.Succeed(pack.Build());

        return Path.Combine(
            projectDirectory,
            "bin",
            _settings.Configuration,
            $"roslyn{analyzerRoslynVersion}",
            $"Pure.DI.{packageVersion.ToString()}.nupkg");
    }
    
    private static void MergeNuGetPackages(IEnumerable<string> mergingPackages, string targetPackage)
    {
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
                    Info($"{entry.FullName,-100} - skipped");
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
                Info($"{entry.FullName,-100} - merged");
            }
        }
    }
}