namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface ITypesMap
    {
        ITypeSymbol ConstructType(ITypeSymbol type);
    }
}