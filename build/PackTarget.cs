// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build;

internal class PackTarget(
    Commands commands,
    ITeamCityArtifactsWriter artifactsWriter,
    [Tag(typeof(CompatibilityCheckTarget))] ITarget<IReadOnlyCollection<Package>> compatibilityCheckTarget)
    : IInitializable, ITarget<IReadOnlyCollection<Package>>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Creates NuGet packages",
        "pack",
        "p");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<Package>> RunAsync(CancellationToken cancellationToken)
    {
        var packages = await compatibilityCheckTarget.RunAsync(cancellationToken);
        foreach (var package in packages)
        {
            WriteLine(package.Path, Color.Details);
            artifactsWriter.PublishArtifact($"{package.Path} => .");
        }

        return packages;
    }
}