namespace Pure.DI.Core;

interface IRefSafety
{
    bool IsRefLike(ITypeSymbol type);

    bool ContainsRefLike(ITypeSymbol type);

    bool IsMaybeRefLike(ITypeSymbol type);

    bool ContainsMaybeRefLike(ITypeSymbol type);

    bool IsScopedParameter(IParameterSymbol parameter);
}
