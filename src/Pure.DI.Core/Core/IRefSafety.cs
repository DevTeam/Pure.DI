namespace Pure.DI.Core;

interface IRefSafety
{
    bool IsRefLike(ITypeSymbol type);

    bool IsMaybeRefLike(ITypeSymbol type);

    bool ContainsMaybeRefLike(ITypeSymbol type);

    bool ContainsMaybeRefLikeValue(ITypeSymbol type);
}
