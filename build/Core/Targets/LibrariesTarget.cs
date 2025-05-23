// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build.Core.Targets;

using NuGet.Versioning;

class LibrariesTarget(
    Settings settings,
    Commands commands,
    Sdk sdk,
    Env env)
    : IInitializable, ITarget<IReadOnlyCollection<Library>>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Build and test libraries", "libs", "l");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<Library>> RunAsync(CancellationToken cancellationToken)
    {
        var version = settings.NextVersion;
        // Libraries
        List<Library> libraries =
        [
            new("Pure.DI.MS",
                new Package(GetPackagePath("Pure.DI.MS", settings.NextVersion), true, version),
                sdk.Versions
                    .Where(i => i.Version.Major >= 7)
                    .Select(v => $"net{v.Version.Major}.{v.Version.Minor}")
                    .ToArray(),
                ["webapi"]),

            new("Pure.DI.Abstractions",
                new Package(GetPackagePath("Pure.DI.Abstractions", settings.NextVersion), true, version),
                sdk.Versions
                    .Where(i => i.Version.Major >= 7)
                    .Select(v => $"net{v.Version.Major}.{v.Version.Minor}")
                    .ToArray(),
                ["classlib"])
        ];

        foreach (var library in libraries)
        {
            await new DotNetPack()
                .WithProps(
                    ("configuration", settings.Configuration),
                    ("version", settings.NextVersion.ToString()))
                .WithConfiguration(settings.Configuration).WithNoBuild(true).WithNoLogo(true)
                .WithProject(Path.Combine(Path.GetFullPath(Path.Combine("src", library.Name)), $"{library.Name}.csproj"))
                .WithOutput(Path.GetDirectoryName(library.Package.Path) ?? "")
                .WithShortName($"packing {library.Name}")
                .BuildAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }

        return libraries;
    }

    private string GetPackagePath(string library, NuGetVersion version)
    {
        var libraryPackageDir = env.GetPath(PathType.PackagesDirectory);
        var libraryPackageName = $"{library}.{version.ToString()}.nupkg";
        return Path.Combine(libraryPackageDir, libraryPackageName);
    }
}