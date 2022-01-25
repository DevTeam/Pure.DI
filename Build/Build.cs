using System.IO.Compression;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;

class Build
{
    private readonly Settings _settings;
    private readonly IBuildRunner _buildRunner;
    private readonly ITeamCityWriter _teamCityWriter;

    public Build(
        Settings settings, 
        IBuildRunner buildRunner,
        ITeamCityWriter teamCityWriter)
    {
        _settings = settings;
        _buildRunner = buildRunner;
        _teamCityWriter = teamCityWriter;
    }

    public string Run()
    {
        var packageVersion = Tools.GetNextVersion(new NuGetRestoreSettings("Pure.DI"), _settings.DefaultVersion);
        
        var packages = _settings.Cases
            .Select(i => CreatePackage(packageVersion, i))
            .ToArray();

        var targetPackage = Path.GetFullPath(
            Path.Combine(
                "Pure.DI",
                "bin",
                "Release",
                $"Pure.DI.{packageVersion}.nupkg"));

        MergeNuGetPackages(packages, targetPackage);
        _teamCityWriter.PublishArtifact($"{targetPackage} => .");
        return targetPackage;
    }
    
    private string CreatePackage(NuGetVersion packageVersion, BuildCase buildCase)
    {
        var analyzerRoslynPackageVersion = buildCase.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        WriteLine($"Build package for Roslyn {analyzerRoslynVersion}.", Color.Highlighted);

        var props = new[]
        {
            ("configuration", "Release"),
            ("version", packageVersion.ToString()!),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString())
        };

        var build = new DotNetBuild()
            .WithProps(props);

        Assertion.Succeed(_buildRunner.Run(build));

        var test = new DotNetTest()
            .WithProps(props)
            .WithNoBuild(true);
        
        Assertion.Succeed(_buildRunner.Run(test));

        var pack = new DotNetPack()
            .WithProps(props)
            .WithNoBuild(true)
            .WithProject(Path.Combine("Pure.DI", "Pure.DI.csproj"));
        
        Assertion.Succeed(_buildRunner.Run(pack));

        return Path.Combine(
            "Pure.DI",
            "bin",
            "Release",
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