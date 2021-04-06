namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface ITypesMap
    {
        int Count { get; }

        void Initialize(ITypeSymbol type, ITypeSymbol targetType);

        ITypeSymbol ConstructType(ITypeSymbol type);
    }
}