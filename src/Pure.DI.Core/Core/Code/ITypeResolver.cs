namespace Pure.DI.Core.Code;

internal interface ITypeResolver
{
    TypeDescription Resolve(ITypeSymbol type);
}