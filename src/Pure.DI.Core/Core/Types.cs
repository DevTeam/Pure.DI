// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal class Types(
    ICache<SpecialTypeKey, INamedTypeSymbol?> specialTypes,
    ICache<MarkerTypeKey, INamedTypeSymbol?> markerTypes)
    : ITypes
{
    private static readonly Dictionary<SpecialType, string> TypeShortNames = new()
    {
        { SpecialType.IAsyncDisposable, $"{nameof(System)}.IAsyncDisposable" },
        { SpecialType.Lock, $"{nameof(System)}.Threading.Lock" },
        { SpecialType.CompositionKind, $"{Names.GeneratorName}.{nameof(CompositionKind)}" },
        { SpecialType.Lifetime, $"{Names.GeneratorName}.{nameof(Lifetime)}" },
        { SpecialType.Tag, $"{Names.GeneratorName}.{nameof(Tag)}" }
    };

    public INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation) =>
        specialTypes.Get(
            new SpecialTypeKey(specialType, compilation),
            i => i.Compilation.GetTypeByMetadataName(TypeShortNames[specialType]));

    public INamedTypeSymbol? GetMarker(int index, Compilation compilation) =>
        index >= 16
            ? null
            : markerTypes.Get(new MarkerTypeKey(index, compilation), i => i.Compilation.GetTypeByMetadataName($"{Names.MarkerTypeName}{(i.Index > 0 ? i.Index.ToString() : "")}"));
}