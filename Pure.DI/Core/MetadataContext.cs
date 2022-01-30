namespace Pure.DI.Core;

internal readonly struct MetadataContext
{
    public readonly Compilation Compilation;
    public readonly CancellationToken CancellationToken;
    public readonly IReadOnlyCollection<Source> Components;
    public readonly IReadOnlyCollection<ResolverMetadata> BaseMetadata;
    public readonly IReadOnlyCollection<ResolverMetadata> CurrentMetadata;
    public readonly long InitDurationMilliseconds;

    public MetadataContext(Compilation compilation,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Source> components,
        IReadOnlyCollection<ResolverMetadata> baseMetadata,
        IReadOnlyCollection<ResolverMetadata> currentMetadata,
        long initDurationMilliseconds)
    {
        Compilation = compilation;
        CancellationToken = cancellationToken;
        Components = components;
        BaseMetadata = baseMetadata;
        CurrentMetadata = currentMetadata;
        InitDurationMilliseconds = initDurationMilliseconds;
    }
}