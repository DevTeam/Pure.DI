namespace Pure.DI.Core;

internal interface IMarker
{
    bool IsMarkerBased(ITypeSymbol type);

    bool IsMarker(ITypeSymbol type);
}