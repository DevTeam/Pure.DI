// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

internal class Marker : IMarker
{
    private const string GenericTypeArgumentPrefix = "TT";

    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    public bool IsMarkerBased(ITypeSymbol type) =>
        IsMarker(type) || type switch
        {
            INamedTypeSymbol { IsGenericType: false } => false,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments.Any(IsMarkerBased),
            IArrayTypeSymbol arrayTypeSymbol => IsMarkerBased(arrayTypeSymbol.ElementType),
            _ => false
        };

    public bool IsMarker(ITypeSymbol type) => 
        type.Name.StartsWith(GenericTypeArgumentPrefix)
        && type.GetAttributes().Any(HasMarketAttribute);

    private static bool HasMarketAttribute(AttributeData attr) => attr.AttributeClass is { Name: nameof(GenericTypeArgumentAttribute) };
}