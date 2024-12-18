// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal class Types(
    ICache<SpecialTypeKey, INamedTypeSymbol?> asyncDisposableTypes)
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
        asyncDisposableTypes.Get(
            new SpecialTypeKey(specialType, compilation),
            i => i.Compilation.GetTypeByMetadataName(TypeShortNames[specialType]));
}