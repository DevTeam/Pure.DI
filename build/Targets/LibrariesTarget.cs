// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf
namespace Build.Targets;

using NuGet.Versioning;

internal class LibrariesTarget(
    Settings settings,
    ICommands commands,
    ISdk sdk)
    : IInitializable, ITarget<IReadOnlyCollection<Library>>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Builds and tests libraries",
        "libs",
        "l");
    
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<Library>> RunAsync(CancellationToken cancellationToken)
    {
        Info("Building libraries");
        
        // Libraries
        List<Library> libraries =
        [
            new Library(
                "Pure.DI.MS",
                new Package(GetPackagePath("Pure.DI.MS", settings.Version), false),
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
                ("version", settings.Version.ToString())
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

        return libraries;
    }

    private string GetPackagePath(string library, NuGetVersion version)
    {
        var libraryProjectDirectory = Path.GetFullPath(Path.Combine("src", library));
        var libraryPackageDir = Path.Combine(libraryProjectDirectory, "bin", settings.Configuration);
        var libraryPackageName = $"{library}.{version.ToString()}.nupkg";
        return Path.Combine(libraryPackageDir, libraryPackageName);
    }
}