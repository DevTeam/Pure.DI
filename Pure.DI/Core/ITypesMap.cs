namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface ITypesMap
    {
        bool Setup(ITypeSymbol type, ITypeSymbol targetType);

        ITypeSymbol ConstructType(ITypeSymbol type);
    }
}