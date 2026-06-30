// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.
namespace Pure.DI.Core;

sealed class MarkerTypeRewriter(ITypeSymbolComparer typeSymbolComparer)
{
    public ITypeSymbol ReplaceTypeParametersWithMarkers(SemanticModel semanticModel, ITypeSymbol typeSymbol)
    {
        var map = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(typeSymbolComparer.Runtime);
        return Replace(typeSymbol);

        ITypeSymbol Replace(ITypeSymbol symbol)
        {
            switch (symbol)
            {
                case ITypeParameterSymbol typeParameter:
                    if (map.TryGetValue(typeParameter, out var markerType))
                    {
                        return markerType;
                    }

                    markerType = GetMarkerType(map.Count) ?? (ITypeSymbol)typeParameter;
                    map[typeParameter] = markerType;
                    return markerType;

                case INamedTypeSymbol { IsGenericType: true } namedType:
                {
                    var args = namedType.TypeArguments.Select(Replace).ToArray();
                    var constructed = namedType.OriginalDefinition.Construct(args);
                    return constructed.WithNullableAnnotation(namedType.NullableAnnotation);
                }

                case IArrayTypeSymbol arrayType:
                {
                    var elementType = Replace(arrayType.ElementType);
                    var result = semanticModel.Compilation.CreateArrayTypeSymbol(elementType, arrayType.Rank);
                    return result.WithNullableAnnotation(arrayType.NullableAnnotation);
                }

                default:
                    return symbol;
            }
        }

        INamedTypeSymbol? GetMarkerType(int index)
        {
            var typeName = index == 0 ? "Pure.DI.TT" : $"Pure.DI.TT{index}";
            return semanticModel.Compilation.GetTypeByMetadataName(typeName);
        }
    }
}
