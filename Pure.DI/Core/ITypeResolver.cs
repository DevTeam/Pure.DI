namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface ITypeResolver
    {
        ITypeSymbol Resolve(ITypeSymbol typeSymbol);
    }
}