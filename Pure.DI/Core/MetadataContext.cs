namespace Pure.DI.Core;

internal readonly struct MetadataContext
{
    public readonly Compilation Compilation;
    public readonly CancellationToken CancellationToken;
    public readonly IEnumerable<Source> Api;
    public readonly IReadOnlyCollection<ResolverMetadata> BaseMetadata;
    public readonly IReadOnlyCollection<ResolverMetadata> CurrentMetadata;
    public readonly long InitDurationMilliseconds;

    public MetadataContext(
        Compilation compilation,
        CancellationToken cancellationToken,
        IEnumerable<Source> api,
        IReadOnlyCollection<ResolverMetadata> baseMetadata,
        IReadOnlyCollection<ResolverMetadata> currentMetadata,
        long initDurationMilliseconds)
    {
        Compilation = compilation;
        CancellationToken = cancellationToken;
        Api = api;
        BaseMetadata = baseMetadata;
        CurrentMetadata = currentMetadata;
        InitDurationMilliseconds = initDurationMilliseconds;
    }
}