// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class Types(
    ICache<SpecialTypeKey, INamedTypeSymbol?> specialTypes,
    ICache<MarkerTypeKey, INamedTypeSymbol?> markerTypes,
    ICache<NameKey, string> names)
    : ITypes, ISymbolNames
{
    private static readonly Dictionary<SpecialType, string> TypeShortNames = new()
    {
        { SpecialType.IAsyncDisposable, $"{nameof(System)}.IAsyncDisposable" },
        { SpecialType.Lock, $"{nameof(System)}.Threading.Lock" },
        { SpecialType.CompositionKind, $"{Names.GeneratorName}.{nameof(CompositionKind)}" },
        { SpecialType.Lifetime, $"{Names.GeneratorName}.{nameof(Lifetime)}" },
        { SpecialType.Tag, $"{Names.GeneratorName}.{nameof(Tag)}" },
        { SpecialType.IConfiguration, $"{Names.GeneratorName}.{nameof(IConfiguration)}" }
    };

    public INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation) =>
        specialTypes.Get(
            new SpecialTypeKey(specialType, compilation),
            i => i.Compilation.GetTypeByMetadataName(TypeShortNames[specialType]));

    public INamedTypeSymbol? GetMarker(int index, Compilation compilation) =>
        index >= 16
            ? null
            : markerTypes.Get(new MarkerTypeKey(index, compilation), i => i.Compilation.GetTypeByMetadataName($"{Names.MarkerTypeName}{(i.Index > 0 ? i.Index.ToString() : "")}"));

    public bool TypeEquals(ISymbol? type1, ISymbol? type2)
    {
        var comparer = SymbolEqualityComparer.Default;
        return comparer.GetHashCode(type1) == comparer.GetHashCode(type2) && comparer.Equals(type1, type2);
    }

    public string GetName(ITypeSymbol typeSymbol) =>
        names.Get(new NameKey(typeSymbol, false), key => key.TypeSymbol.ToString());

    public string GetGlobalName(ITypeSymbol typeSymbol) =>
        names.Get(new NameKey(typeSymbol, true), key => key.TypeSymbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));
}