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

    public bool IsMaybeRefLike(ITypeSymbol type) =>
        IsRefLike(type)
#if ROSLYN5_6_OR_GREATER
        || type is ITypeParameterSymbol { AllowsRefLikeType: true }
#endif
        ;

    public bool ContainsMaybeRefLike(ITypeSymbol type)
    {
        if (IsMaybeRefLike(type))
        {
            return true;
        }

        switch (type)
        {
            case INamedTypeSymbol namedType:
                return namedType.TypeArguments.Any(ContainsMaybeRefLike);

            case IArrayTypeSymbol arrayType:
                return ContainsMaybeRefLike(arrayType.ElementType);

            case IPointerTypeSymbol pointerType:
                return ContainsMaybeRefLike(pointerType.PointedAtType);

            default:
                return false;
        }
    }

    public bool ContainsMaybeRefLikeValue(ITypeSymbol type)
    {
        if (IsMaybeRefLike(type))
        {
            return true;
        }

        switch (type)
        {
            case INamedTypeSymbol { TypeKind: TypeKind.Delegate }:
                return false;

            case INamedTypeSymbol namedType:
                return namedType.TypeArguments.Any(ContainsMaybeRefLikeValue);

            case IArrayTypeSymbol arrayType:
                return ContainsMaybeRefLikeValue(arrayType.ElementType);

            case IPointerTypeSymbol pointerType:
                return ContainsMaybeRefLikeValue(pointerType.PointedAtType);

            default:
                return false;
        }
    }

    public bool IsScopedParameter(IParameterSymbol parameter) =>
#if ROSLYN5_6_OR_GREATER
        parameter.ScopedKind != ScopedKind.None;
#else
        false;
#endif
}
