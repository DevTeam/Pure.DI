// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build;

using NuGet.Versioning;

internal class LibrariesTarget(
    Settings settings,
    Commands commands,
    Sdk sdk)
    : IInitializable, ITarget<IReadOnlyCollection<Library>>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Builds and tests libraries",
        "libs",
        "l");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public Task<IReadOnlyCollection<Library>> RunAsync(CancellationToken cancellationToken)
    {
        // Libraries
        List<Library> libraries =
        [
            new Library(
                "Pure.DI.MS",
                new Package(GetPackagePath("Pure.DI.MS", settings.NextVersion), false),
                sdk.Versions
                    .Where(i => i.Version.Major >= 7)
                    .Select(v => $"net{v.Version.Major}.{v.Version.Minor}")
                    .ToArray(),
                ["webapi"]),
            
            new Library(
                "Pure.DI.Abstractions",
                new Package(GetPackagePath("Pure.DI.Abstractions", settings.NextVersion), false),
                sdk.Versions
                    .Where(i => i.Version.Major >= 7)
                    .Select(v => $"net{v.Version.Major}.{v.Version.Minor}")
                    .ToArray(),
                ["classlib"])
        ];

        foreach (var library in libraries)
        {
            var props = new[]
            {
                ("configuration", settings.Configuration),
                ("version", settings.NextVersion.ToString())
            };

            new DotNetPack()
                .WithProps(props)
                .WithConfiguration(settings.Configuration)
                .WithNoBuild(true)
                .WithNoLogo(true)
                .WithProject(Path.Combine(Path.GetFullPath(Path.Combine("src", library.Name)), $"{library.Name}.csproj"))
                .Build()
                .Succeed();
        }

        return Task.FromResult<IReadOnlyCollection<Library>>(libraries);
    }

    private string GetPackagePath(string library, NuGetVersion version)
    {
        var libraryProjectDirectory = Path.GetFullPath(Path.Combine("src", library));
        var libraryPackageDir = Path.Combine(libraryProjectDirectory, "bin", settings.Configuration);
        var libraryPackageName = $"{library}.{version.ToString()}.nupkg";
        return Path.Combine(libraryPackageDir, libraryPackageName);
    }
}