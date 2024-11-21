namespace Pure.DI.Core;

internal interface ITypes
{
    INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation);
}