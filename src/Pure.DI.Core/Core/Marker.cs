// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Marker(IGenericTypeArguments genericTypeArguments) : IMarker
{
    public bool IsMarkerBased(MdSetup setup, ITypeSymbol type) =>
        IsMarker(setup, type) || type switch
        {
            INamedTypeSymbol { IsGenericType: false } => false,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments.Any(i => IsMarkerBased(setup, i)),
            IArrayTypeSymbol arrayTypeSymbol => IsMarkerBased(setup, arrayTypeSymbol.ElementType),
            _ => false
        };

    public bool IsMarker(MdSetup setup, ITypeSymbol type) =>
        genericTypeArguments.IsGenericTypeArgument(setup, type)
        || type.GetAttributes()
            .Where(i => i.AttributeClass is not null)
            .Any(i => genericTypeArguments.IsGenericTypeArgumentAttribute(setup, i.AttributeClass!));
}