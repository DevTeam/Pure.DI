// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class RefSafety : IRefSafety
{
    public bool IsRefLike(ITypeSymbol type) => type.IsRefLikeType;

    public bool ContainsRefLike(ITypeSymbol type)
    {
        if (type.IsRefLikeType)
        {
            return true;
        }

        switch (type)
        {
            case INamedTypeSymbol namedType:
                return namedType.TypeArguments.Any(ContainsRefLike);

            case IArrayTypeSymbol arrayType:
                return ContainsRefLike(arrayType.ElementType);

            case IPointerTypeSymbol pointerType:
                return ContainsRefLike(pointerType.PointedAtType);

            default:
                return false;
        }
    }

    public bool IsScopedParameter(IParameterSymbol parameter) =>
        parameter.ScopedKind != ScopedKind.None;
}
