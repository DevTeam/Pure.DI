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
    [Tag(typeof(CompatibilityCheckTarget))] ITarget<BuildResult> compatibilityCheckTarget)
    : IInitializable, ITarget<BuildResult>
{
    public ValueTask InitializeAsync() => commands.Register(
        this,
        "Creates NuGet packages",
        "pack",
        "p");

    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async ValueTask<BuildResult> RunAsync(CancellationToken cancellationToken)
    {
        Info("Packing");
        var buildResult = await compatibilityCheckTarget.RunAsync(cancellationToken);
        artifactsWriter.PublishArtifact($"{buildResult.GeneratorPackage} => .");

        // Libraries
        foreach (var library in buildResult.Libraries)
        {
            artifactsWriter.PublishArtifact($"{library.PackagePath} => .");
        }

        return buildResult;
    }
}