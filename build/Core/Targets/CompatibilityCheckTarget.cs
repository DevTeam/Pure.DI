// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build.Core.Targets;

using NuGet.Versioning;

class CompatibilityCheckTarget(
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
        await new DotNetNewInstall()
            .WithPackage("Pure.DI.Templates")
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        var baseFrameworks = Enumerable.Range(5, settings.BaseDotNetFrameworkMajorVersion - 4).Select(i => $"net{i}.0");
        List<string> frameworks =
        [
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

        var tasks = new List<Task>();
        tasks.AddRange(
            baseFrameworks
                .Concat(frameworks)
                .Reverse()
                .Select(framework => CompatibilityCheckAsync(generatorPackage.Path, framework, cancellationToken)));

        // Libraries
        var packages = new List<Package> { generatorPackage };
        foreach (var library in libraries)
        {
            tasks.Add(CompatibilityCheckAsync(generatorPackage.Path, library, cancellationToken));
            packages.Add(library.Package);
        }

        await Task.WhenAll(tasks);
        return packages;
    }

    private async Task CompatibilityCheckAsync(
        string generatorPackage,
        string framework,
        CancellationToken cancellationToken)
    {
        var tempDirectory = env.GetPath(PathType.TempDirectory);
        Summary("Compatibility check for ", framework.WithColor(Color.Details));
        try
        {
            tempDirectory = Path.Combine(tempDirectory, "src", framework);
            await new DotNetNew()
                .WithTemplateName("dilib")
                .WithName("MyApp")
                .WithOutput(tempDirectory)
                .WithForce(true)
                .WithFramework(framework)
                .WithShortName($"creating the project from the dilib template for {framework}")
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            await new DotNetRestore()
                .WithShortName($"restoring the project for {framework}")
                .WithWorkingDirectory(tempDirectory)
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            if (!framework.Contains('.'))
            {
                await new DotNetAddPackage()
                    .WithProject(tempDirectory)
                    .WithPackage("Microsoft.NETFramework.ReferenceAssemblies")
                    .WithShortName($"adding the package Microsoft.NETFramework.ReferenceAssemblies for {framework}")
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
            }

            await new DotNetAddPackage()
                .WithProject(tempDirectory)
                .WithPackage("Pure.DI")
                .WithVersion(settings.NextVersion.ToString())
                .AddSources(Path.GetDirectoryName(generatorPackage)!)
                .WithShortName($"adding the the package {Path.GetFileName(generatorPackage)} for {framework}")
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            await new DotNetBuild().WithWorkingDirectory(tempDirectory)
                .WithShortName($"building the sample project for {framework}")
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
                Summary($"Testing {library.Name} library under {framework}.");
                var tempDirForFramework = tempDirectory + "_" + framework;
                Directory.CreateDirectory(tempDirForFramework);
                await new DotNetNew()
                    .WithTemplateName(templateName)
                    .WithName("MyApp")
                    .WithOutput(tempDirForFramework)
                    .WithFramework(framework)
                    .WithShortName($"creating the project from the {templateName} template for {framework}")
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetAddPackage()
                    .WithProject(tempDirForFramework)
                    .WithPackage("Pure.DI")
                    .WithNoRestore(true)
                    .WithVersion(settings.NextVersion.ToString())
                    .AddSources(Path.GetDirectoryName(generatorPackage)!)
                    .WithFramework(framework)
                    .WithShortName($"addong the package {Path.GetFileName(generatorPackage)} for the {templateName} template for {framework}")
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                var libraryPackageDir = Path.GetDirectoryName(library.Package.Path)!;
                DeleteNuGetPackageFromCache(library.Name, settings.NextVersion, libraryPackageDir);

                await new DotNetAddPackage()
                    .WithProject(tempDirForFramework)
                    .WithPackage(library.Name)
                    .WithNoRestore(true)
                    .WithVersion(settings.NextVersion.ToString())
                    .AddSources(libraryPackageDir)
                    .WithFramework(framework)
                    .WithShortName($"adding the package {library.Name} for the {templateName} template for {framework}")
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetRestore()
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithForce(true)
                    .WithNoCache(true)
                    .AddSources(Path.GetDirectoryName(generatorPackage)!, libraryPackageDir)
                    .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

                await new DotNetBuild()
                    .WithWorkingDirectory(tempDirForFramework)
                    .WithNoRestore(true)
                    .WithShortName($"building the {templateName} template for {framework}")
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