// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build;

using NuGet.Versioning;

internal class CompatibilityCheckTarget(
    Settings settings,
    Commands commands,
    Env env,
    INuGet nuGet,
    [Tag(typeof(GeneratorTarget))] ITarget<Package> generatorTarget,
    [Tag(typeof(LibrariesTarget))] ITarget<IReadOnlyCollection<Library>> librariesTarget)
    : IInitializable, ITarget<IReadOnlyCollection<Package>>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Compatibility checks", "check", "c");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<Package>> RunAsync(CancellationToken cancellationToken)
    {
        var generatorPackage = await generatorTarget.RunAsync(cancellationToken);
        var libraries = await librariesTarget.RunAsync(cancellationToken);

        DeleteNuGetPackageFromCache("Pure.DI", settings.NextVersion, Path.GetDirectoryName(generatorPackage.Path)!);
        await new DotNetCustom("new", "install", "Pure.DI.Templates")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
        
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
            "netstandard1.3",
            "netstandard1.4",
            "netstandard1.5",
            "netstandard1.6",
            "netstandard2.0",
            "netstandard2.1",
            "net48",
            "net45",
            "net35",
            "net20"
        ];

        foreach (var framework in frameworks.Reverse())
        {
            await CompatibilityCheckAsync(generatorPackage.Path, framework, cancellationToken);
        }

        var packages = new List<Package> { generatorPackage };

        // Libraries
        foreach (var library in libraries)
        {
            await CompatibilityCheckAsync(generatorPackage.Path, library, cancellationToken);
            packages.Add(library.Package);
        }

        return packages;
    }

    private async Task CompatibilityCheckAsync(
        string generatorPackage,
        string framework,
        CancellationToken cancellationToken)
    {
        var tempDirectory = env.GetPath(PathType.TempDirectory);
        Info($"Compatibility check for {framework}.");
        try
        {
            tempDirectory = Path.Combine(tempDirectory, "src", framework);
            await new DotNetNew(
                    "dilib",
                    "-n",
                    "MyApp",
                    "-o",
                    tempDirectory,
                    "--force",
                    "-f", framework)
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            await new DotNetRestore().WithWorkingDirectory(tempDirectory).RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            if (!framework.Contains('.'))
            {
                await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirectory),
                        "package",
                        "Microsoft.NETFramework.ReferenceAssemblies")
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
            }

            await new DotNetCustom(
                    "add",
                    Path.Combine(tempDirectory),
                    "package",
                    "Pure.DI",
                    "-v",
                    settings.NextVersion.ToString(),
                    "-s",
                    Path.GetDirectoryName(generatorPackage)!)
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            await new DotNetBuild().WithWorkingDirectory(tempDirectory)
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private async Task CompatibilityCheckAsync(string generatorPackage, Library library, CancellationToken cancellationToken)
    {
        var tempDirectory = env.GetPath(PathType.TempDirectory);
        try
        {
            foreach (var templateName in library.TemplateNames)
            foreach (var framework in library.Frameworks)
            {
                Info($"Testing {library.Name} library under {framework}.");
                var tempDirForFramework = tempDirectory + "_" + framework;
                Directory.CreateDirectory(tempDirForFramework);
                await new DotNetNew(
                        templateName,
                        "-n",
                        "MyApp",
                        "-o",
                        tempDirForFramework,
                        "--force",
                        "-f", framework)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirForFramework),
                        "package",
                        "Pure.DI",
                        "-n",
                        "-v",
                        settings.NextVersion.ToString(),
                        "-f",
                        framework,
                        "-s",
                        Path.GetDirectoryName(generatorPackage)!)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                var libraryPackageDir = Path.GetDirectoryName(library.Package.Path)!;
                DeleteNuGetPackageFromCache(library.Name, settings.NextVersion, libraryPackageDir);

                await new DotNetCustom(
                        "add",
                        Path.Combine(tempDirForFramework),
                        "package",
                        library.Name,
                        "-n",
                        "-v",
                        settings.NextVersion.ToString(),
                        "-s",
                        libraryPackageDir,
                        "-f",
                        framework)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetRestore()
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithForce(true)
                    .WithNoCache(true)
                    .AddSources(Path.GetDirectoryName(generatorPackage)!, libraryPackageDir)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetBuild()
                    .WithShortName($"Build for compatibility check of {framework} for template {templateName}")
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithNoRestore(true)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
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