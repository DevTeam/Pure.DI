namespace Pure.DI.Core;

internal interface IMarker
{
    bool IsMarkerBased(MdSetup setup, ITypeSymbol type);

    bool IsMarker(MdSetup setup, ITypeSymbol type);
}