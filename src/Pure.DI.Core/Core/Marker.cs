// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Marker(
    IGenericTypeArguments genericTypeArguments,
    ICache<Marker.BasedMarkerKey, bool> markerBasedCache,
    ICache<Marker.MarkerKey, bool> markerCache): IMarker
{
    public bool IsMarkerBased(MdSetup setup, ITypeSymbol type) =>
        markerBasedCache.Get(new BasedMarkerKey(type), _ => IsMarkerBasedInternal(setup, type));

    public bool IsMarker(MdSetup setup, ITypeSymbol type) =>
        markerCache.Get(new MarkerKey(type), _ => IsMarkerInternal(setup, type));

    private bool IsMarkerBasedInternal(MdSetup setup, ITypeSymbol type) =>
        IsMarker(setup, type) || type switch
        {
            INamedTypeSymbol { IsGenericType: false } => false,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments.Any(i => IsMarkerBased(setup, i)),
            IArrayTypeSymbol arrayTypeSymbol => IsMarkerBased(setup, arrayTypeSymbol.ElementType),
            _ => false
        };

    private bool IsMarkerInternal(MdSetup setup, ITypeSymbol type) =>
        genericTypeArguments.IsGenericTypeArgument(setup, type)
        || type.GetAttributes()
            .Where(i => i.AttributeClass is not null)
            .Any(i => genericTypeArguments.IsGenericTypeArgumentAttribute(setup, i.AttributeClass!));

    internal class MarkerKeyBase(ITypeSymbol typeSymbol)
    {
        private readonly ITypeSymbol _typeSymbol = typeSymbol;

        public override bool Equals(object? obj) =>
            SymbolEqualityComparer.Default.Equals(_typeSymbol, (obj as MarkerKeyBase)?._typeSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(_typeSymbol);
    }

    internal class BasedMarkerKey(ITypeSymbol typeSymbol) : MarkerKeyBase(typeSymbol);

    internal class MarkerKey(ITypeSymbol typeSymbol) : MarkerKeyBase(typeSymbol);
}