// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class RefSafety : IRefSafety
{
    public bool IsRefLike(ITypeSymbol type) => type.IsRefLikeType;

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

        return type switch
        {
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsMaybeRefLike),
            IArrayTypeSymbol arrayType => ContainsMaybeRefLike(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsMaybeRefLike(pointerType.PointedAtType),
            _ => false
        };
    }

    public bool ContainsMaybeRefLikeValue(ITypeSymbol type)
    {
        if (IsMaybeRefLike(type))
        {
            return true;
        }

        return type switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Delegate } => false,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsMaybeRefLikeValue),
            IArrayTypeSymbol arrayType => ContainsMaybeRefLikeValue(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsMaybeRefLikeValue(pointerType.PointedAtType),
            _ => false
        };
    }
}
