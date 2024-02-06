// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build.Targets;

using NuGet.Versioning;

internal class CompatibilityCheckTarget(
    ICommands commands,
    IPaths paths,
    INuGet nuGet,
    [Tag(typeof(TestTarget))] ITarget<BuildResult> testTarget)
    : IInitializable, ITarget<BuildResult>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Compatibility checks",
        "check",
        "c");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<BuildResult> RunAsync(CancellationToken cancellationToken)
    {
        Info("Compatibility checks");
        var buildResult = await testTarget.RunAsync(cancellationToken);

        DeleteNuGetPackageFromCache("Pure.DI", buildResult.GeneratorPackageVersion, Path.GetDirectoryName(buildResult.GeneratorPackage)!);

        var exitCode = await new DotNetCustom("new", "-i", "Pure.DI.Templates").RunAsync(cancellationToken: cancellationToken);
        if (exitCode != 0)
        {
            throw new InvalidOperationException("Cannot install the project template \"Pure.DI.Templates\".");
        }

        string[] frameworks =
        [
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
        ];

        foreach (var framework in frameworks)
        {
            await CompatibilityCheckAsync(buildResult, framework, cancellationToken);
        }

        // Libraries
        foreach (var library in buildResult.Libraries)
        {
            await CompatibilityCheckAsync(buildResult, library);
        }

        return buildResult;
    }

    private async Task CompatibilityCheckAsync(
        BuildResult buildResult,
        string framework,
        CancellationToken cancellationToken)
    {
        var tempDirectory = paths.TempDirectory;
        Info($"Compatibility check for {framework}.");
        try
        {
            tempDirectory = Path.Combine(tempDirectory, "src", framework);
            var exitCode = await new DotNetNew(
                    "dilib",
                    "-n",
                    "MyApp",
                    "-o",
                    tempDirectory,
                    "--force",
                    "-f", framework)
                .RunAsync(cancellationToken: cancellationToken);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Cannot create app for \"{framework}\".");
            }

            exitCode = await new DotNetRestore().WithWorkingDirectory(tempDirectory).RunAsync(cancellationToken: cancellationToken);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Cannot restore for \"{framework}\".");
            }

            if (!framework.Contains('.'))
            {
                exitCode = await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirectory),
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
                    Path.Combine(tempDirectory),
                    "package",
                    "Pure.DI",
                    "-v",
                    buildResult.GeneratorPackageVersion.ToString(),
                    "-s",
                    Path.GetDirectoryName(buildResult.GeneratorPackage)!)
                .RunAsync(cancellationToken: cancellationToken);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Cannot add \"Pure.DI\" package of \"{framework}\".");
            }

            exitCode = await new DotNetBuild().WithWorkingDirectory(tempDirectory).RunAsync(cancellationToken: cancellationToken);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Cannot build app for \"{framework}\".");
            }
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private async Task CompatibilityCheckAsync(BuildResult buildResult, Library library)
    {
        var tempDirectory = paths.TempDirectory;
        try
        {
            foreach (var templateName in library.TemplateNames)
            foreach (var framework in library.Frameworks)
            {
                Info($"Testing {library.Name} library under {framework}.");
                var tempDirForFramework = tempDirectory + "_" + framework;
                Directory.CreateDirectory(tempDirForFramework);
                var exitCode = await new DotNetNew(
                        templateName,
                        "-n",
                        "MyApp",
                        "-o",
                        tempDirForFramework,
                        "--force",
                        "-f", framework)
                    .RunAsync();

                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Cannot create app from the template {templateName}.");
                }

                exitCode = await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirForFramework),
                        "package",
                        "Pure.DI",
                        "-n",
                        "-v",
                        buildResult.GeneratorPackageVersion.ToString(),
                        "-f",
                        framework,
                        "-s",
                        Path.GetDirectoryName(buildResult.GeneratorPackage)!)
                    .RunAsync();

                if (exitCode != 0)
                {
                    throw new InvalidOperationException("Cannot add the NuGet package reference.");
                }

                var libraryPackageDir = Path.GetDirectoryName(library.PackagePath)!;
                DeleteNuGetPackageFromCache(library.Name, buildResult.GeneratorPackageVersion, libraryPackageDir);

                exitCode = await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirForFramework),
                        "package",
                        library.Name,
                        "-n",
                        "-v",
                        buildResult.GeneratorPackageVersion.ToString(),
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
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithForce(true)
                    .WithNoCache(true)
                    .AddSources(Path.GetDirectoryName(buildResult.GeneratorPackage)!, libraryPackageDir)
                    .RunAsync();

                if (exitCode != 0)
                {
                    throw new InvalidOperationException("Cannot restore NuGet package.");
                }

                exitCode = await new DotNetBuild()
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithNoRestore(true)
                    .RunAsync();

                if (exitCode != 0)
                {
                    throw new InvalidOperationException("Cannot build.");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private void DeleteNuGetPackageFromCache(string packageId, NuGetVersion minVersion, params string[] sources)
    {
        var packagePaths = nuGet.Restore(
                new NuGetRestoreSettings(packageId)
                    .WithSources(sources)
                    .WithVersionRange(new VersionRange(minVersion)))
            .Where(i => i.Name == packageId)
            .Select(i => i.Path);

        foreach (var path in packagePaths)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}