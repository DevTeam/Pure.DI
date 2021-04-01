namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface ITypesMap
    {
        INamedTypeSymbol ConstructType(INamedTypeSymbol type);
    }
}