// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;

internal class PackTarget: Command, ITarget<IReadOnlyCollection<string>>
{
    private readonly Settings _settings;
    private readonly ITeamCityWriter _teamCityWriter;
    private readonly INuGet _nuGet;

    public PackTarget(
        Settings settings,
        ITeamCityWriter teamCityWriter,
        INuGet nuGet)
        : base("pack", "Creates NuGet packages")
    {
        _settings = settings;
        _teamCityWriter = teamCityWriter;
        _nuGet = nuGet;
        this.SetHandler(RunAsync);
        AddAlias("p");
    }
    
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<string>> RunAsync(InvocationContext ctx)
    {
        var packages = new List<string>();
        
        // Pure.DI generator package
        var generatorPackageVersion = _settings.VersionOverride ?? new NuGetRestoreSettings("Pure.DI").GetNextVersion(_settings.VersionRange);
        var generatorProjectDirectory = Path.Combine("src", "Pure.DI");
        var generatorPackages = _settings.CodeAnalysis
            .Select(codeAnalysis => CreateGeneratorPackage(generatorPackageVersion, codeAnalysis, generatorProjectDirectory));

        var generatorPackage = Path.GetFullPath(MergeGeneratorPackages(generatorPackageVersion, generatorPackages, generatorProjectDirectory));
        _teamCityWriter.PublishArtifact($"{generatorPackage} => .");
        packages.Add(generatorPackage);
        DeleteNuGetPackageFromCache("Pure.DI", generatorPackageVersion, Path.GetDirectoryName(generatorPackage)!);

        var exitCode = await new DotNetCustom("new", "-i", "Pure.DI.Templates").RunAsync();
        if (exitCode != 0)
        {
            throw new InvalidOperationException("Cannot install the project template \"Pure.DI.Templates\".");
        }

        var tempDir2 = GetTempDir();
        try
        {
            var frameworks = new[]
                {
                    "net8.0",
                    "net7.0",
                    "net6.0",
                    "net5.0",
                    "netcoreapp3.1",
                    "netcoreapp3.0",
                    "netcoreapp2.2",
                    "netcoreapp2.1",
                    "netcoreapp2.0",
                    "netcoreapp1.1",
                    "netcoreapp1.0",
                    "net48",
                    "net45",
                    "net35",
                    "net20",
                    "netstandard1.3",
                    "netstandard1.4",
                    "netstandard1.5",
                    "netstandard1.6",
                    "netstandard2.0",
                    "netstandard2.1"
                };

            foreach (var framework in frameworks)
            {
                await TestForFrameworkAsync(tempDir2, framework, generatorPackageVersion, generatorPackage, ctx.GetCancellationToken());
            }
        }
        finally
        {
            Directory.Delete(tempDir2, true);
        }

        // Libraries
        var libraries = new[]
        {
            new Library(
                "Pure.DI.MS",
                ["net8.0", "net7.0"],
                ["webapi"])
        };

        foreach (var library in libraries)
        {
            var props = new[]
            {
                ("configuration", _settings.Configuration),
                ("version", generatorPackageVersion.ToString())
            };

            var libraryProjectDirectory = Path.GetFullPath(Path.Combine("src", library.Name));
            var libraryPackResult = await new DotNetPack()
                .WithProps(props)
                .WithConfiguration(_settings.Configuration)
                .WithNoBuild(true)
                .WithNoLogo(true)
                .WithProject(Path.Combine(libraryProjectDirectory, $"{library.Name}.csproj"))
                .BuildAsync();
        
            libraryPackResult.Succeed();
        
            var libraryPackageDir = Path.Combine(libraryProjectDirectory, "bin", _settings.Configuration);
            var libraryPackageName = $"{library.Name}.{generatorPackageVersion.ToString()}.nupkg";
            var libraryPackage = Path.Combine(libraryPackageDir, libraryPackageName);
            foreach (var templateName in library.TemplateNames)
            foreach (var framework in library.Frameworks)
            {
                var tempDir = GetTempDir() + "_"  + framework;
                Directory.CreateDirectory(tempDir);
                try
                {
                    exitCode = await new DotNetNew(
                            templateName,
                            "-n",
                            "MyApp",
                            "-o",
                            tempDir,
                            "--force",
                            "-f", framework)
                        .RunAsync();
                    
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException($"Cannot create app from the template {templateName}.");
                    }
                    
                    exitCode = await new DotNetCustom(
                            "add",
                            Path.Combine(tempDir),
                            "package",
                            "Pure.DI",
                            "-n",
                            "-v",
                            generatorPackageVersion.ToString(),
                            "-f",
                            framework,
                            "-s",
                            Path.GetDirectoryName(generatorPackage)!)
                        .RunAsync();
                    
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException("Cannot add the NuGet package reference.");
                    }

                    DeleteNuGetPackageFromCache(library.Name, generatorPackageVersion, libraryPackageDir);
                    
                    exitCode = await new DotNetCustom(
                            "add",
                            Path.Combine(tempDir),
                            "package",
                            library.Name,
                            "-n",
                            "-v",
                            generatorPackageVersion.ToString(),
                            "-s",
                            libraryPackageDir,
                            "-f",
                            framework)
                        .RunAsync();
                    
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException("Cannot add the NuGet package reference.");
                    }

                    exitCode = await new DotNetRestore()
                        .WithWorkingDirectory(tempDir)
                        .WithForce(true)
                        .WithNoCache(true)
                        .AddSources(Path.GetDirectoryName(generatorPackage)!, libraryPackageDir)
                        .RunAsync();
                    
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException("Cannot restore NuGet package.");
                    }
                    
                    exitCode = await new DotNetBuild()
                        .WithWorkingDirectory(tempDir)
                        .WithNoRestore(true)
                        .RunAsync();

                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException("Cannot build.");
                    }
                }
                finally
                {
                    Directory.Delete(tempDir, true);
                }
            }
            
            _teamCityWriter.PublishArtifact($"{libraryPackage} => .");

        }
        
        return packages;
    }

    private static async Task TestForFrameworkAsync(
        string tempDir,
        string framework,
        NuGetVersion generatorPackageVersion,
        string generatorPackage,
        CancellationToken cancellationToken)
    {
        tempDir = Path.Combine(tempDir, "src", framework);
        var exitCode = await new DotNetNew(
                "dilib",
                "-n",
                "MyApp",
                "-o",
                tempDir,
                "--force",
                "-f", framework)
            .RunAsync(cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Cannot create app for \"{framework}\".");
        }

        exitCode =await new DotNetRestore().WithWorkingDirectory(tempDir).RunAsync(cancellationToken: cancellationToken);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Cannot restore for \"{framework}\".");
        }
        
        if (!framework.Contains('.'))
        {
            exitCode = await new DotNetCustom(
                    "add",
                    Path.Combine(tempDir),
                    "package",
                    "Microsoft.NETFramework.ReferenceAssemblies")
                .RunAsync(cancellationToken: cancellationToken);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Cannot add \"Microsoft.NETFramework.ReferenceAssemblies\" package of \"{framework}\".");
            }
        }

        exitCode = await new DotNetCustom(
                "add",
                Path.Combine(tempDir),
                "package",
                "Pure.DI",
                "-v",
                generatorPackageVersion.ToString(),
                "-s",
                Path.GetDirectoryName(generatorPackage)!)
            .RunAsync(cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Cannot add \"Pure.DI\" package of \"{framework}\".");
        }

        exitCode = await new DotNetBuild().WithWorkingDirectory(tempDir).RunAsync(cancellationToken: cancellationToken);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Cannot build app for \"{framework}\".");
        }
    }

    private static string GetTempDir() => 
        Path.Combine(Path.GetTempPath(), $"Pure.DI_{Guid.NewGuid().ToString()[..4]}");

    private void DeleteNuGetPackageFromCache(string packageId, NuGetVersion minVersion, params string[] sources)
    {
        var paths = _nuGet.Restore(
                new NuGetRestoreSettings(packageId)
                    .WithSources(sources)
                    .WithVersionRange(new VersionRange(minVersion)))
            .Where(i => i.Name == packageId)
            .Select(i => i.Path);

        foreach (var path in paths)
        {
            Directory.Delete(path, true);
        }
    }

    private record Library(
        string Name,
        string[] Frameworks,
        string[] TemplateNames);
    
    private string CreateGeneratorPackage(NuGetVersion packageVersion, CodeAnalysis codeAnalysis, string projectDirectory)
    {
        var analyzerRoslynPackageVersion = codeAnalysis.AnalyzerRoslynPackageVersion;
        var analyzerRoslynVersion = new Version(analyzerRoslynPackageVersion.Major, analyzerRoslynPackageVersion.Minor);
        WriteLine($"Build package for Roslyn {analyzerRoslynVersion}.", Color.Highlighted);

        var props = new[]
        {
            ("configuration", _settings.Configuration),
            ("version", packageVersion.ToString()),
            ("AnalyzerRoslynVersion", analyzerRoslynVersion.ToString()),
            ("AnalyzerRoslynPackageVersion", analyzerRoslynPackageVersion.ToString())
        };
        
        var build = new MSBuild()
            .WithTarget("clean;rebuild")
            .WithRestore(true)
            .WithProps(props);
        
        var buildResult = build.Build();
        buildResult.Succeed();

        var test = new DotNetTest()
            .WithProps(props)
            .WithConfiguration(_settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true);

        var testResult = test.Build();
        WriteLine(testResult.ToString(), Color.Highlighted);
        testResult.Succeed();

        var pack = new DotNetPack()
            .WithProps(props)
            .WithConfiguration(_settings.Configuration)
            .WithNoBuild(true)
            .WithNoLogo(true)
            .WithProject(Path.Combine(projectDirectory, "Pure.DI.csproj"));
        
        pack.Build().Succeed();
        return Path.Combine(projectDirectory, "bin", $"roslyn{analyzerRoslynVersion}", _settings.Configuration, $"Pure.DI.{packageVersion.ToString()}.nupkg");
    }
    
    private string MergeGeneratorPackages(NuGetVersion packageVersion, IEnumerable<string> mergingPackages, string projectDirectory)
    {
        var targetPackage = Path.GetFullPath(Path.Combine(projectDirectory, "bin", _settings.Configuration, $"Pure.DI.{packageVersion}.nupkg"));
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
        
        return targetPackage;
    }
}