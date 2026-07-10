namespace Pure.DI.Core;

interface IRefSafety
{
    bool IsRefLike(ITypeSymbol type);

    bool ContainsRefLike(ITypeSymbol type);

    bool IsScopedParameter(IParameterSymbol parameter);
}
