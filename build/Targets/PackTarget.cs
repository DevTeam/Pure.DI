// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable InvertIf

namespace Build.Targets;

internal class PackTarget(
    ICommands commands,
    ITeamCityArtifactsWriter artifactsWriter,
    [Tag(typeof(CompatibilityCheckTarget))] ITarget<IReadOnlyCollection<string>> compatibilityCheckTarget)
    : IInitializable, ITarget<IReadOnlyCollection<string>>
{
    public Task InitializeAsync() => commands.Register(
        this,
        "Creates NuGet packages",
        "pack",
        "p");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task<IReadOnlyCollection<string>> RunAsync(CancellationToken cancellationToken)
    {
        Info("Packing");
        var packages = await compatibilityCheckTarget.RunAsync(cancellationToken);
        foreach (var package in packages)
        {
            artifactsWriter.PublishArtifact($"{package} => .");
        }

        return packages;
    }
}