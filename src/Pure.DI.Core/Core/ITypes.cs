namespace Pure.DI.Core;

interface ITypes
{
    INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation);

    bool TypeEquals(ISymbol? type1, ISymbol? type2);

    bool IsUnionType(Compilation compilation, ITypeSymbol type);

    bool IsImplicitUnionConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);
}