namespace Pure.DI.InterfaceGeneration;

sealed class GeneratedInterfacesResult(
    ImmutableArray<GeneratedInterfaceSource> sources,
    ImmutableArray<GeneratedInterfaceWarning> warnings)
{
    public ImmutableArray<GeneratedInterfaceSource> Sources { get; } = sources;

    public ImmutableArray<GeneratedInterfaceWarning> Warnings { get; } = warnings;
}
