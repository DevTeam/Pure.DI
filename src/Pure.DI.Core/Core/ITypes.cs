namespace Pure.DI.Core;

internal interface ITypes
{
    INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation);

    INamedTypeSymbol? GetMarker(int index, Compilation compilation);
}