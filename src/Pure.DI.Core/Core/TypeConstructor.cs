// ReSharper disable InvertIf
// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace Pure.DI.Core;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
sealed class TypeConstructor(
    IMarker marker,
    ITypes types,
    ITypeSymbolComparer typeSymbolComparer)
    : ITypeConstructor
{
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _map = new(typeSymbolComparer.Runtime);
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _reversedMap = new(typeSymbolComparer.Runtime);

    public bool TryBind(MdSetup setup, ITypeSymbol source, ITypeSymbol target)
    {
        var map = _map.ToArray();
        _reversedMap.Clear();
        if (TryBindCore(setup, source, target))
        {
            return true;
        }

        _map.Clear();
        foreach (var item in map)
        {
            _map[item.Key] = item.Value;
        }

        return false;
    }

    private bool TryBindCore(MdSetup setup, ITypeSymbol source, ITypeSymbol target)
    {
        if (!IsNullableReferenceTypeCompatible(source, target))
        {
            return false;
        }

        if (marker.IsMarker(setup, source))
        {
            _map[source] = target;
            return true;
        }

        var result = true;
        switch (source)
        {
            case INamedTypeSymbol sourceNamedType when target is INamedTypeSymbol targetNamedType:
            {
                if (!types.TypeEquals(source.OriginalDefinition, target.OriginalDefinition))
                {
                    return false;
                }

                if (!sourceNamedType.IsGenericType)
                {
                    return types.TypeEquals(source, target);
                }

                if (_map.ContainsKey(source))
                {
                    return true;
                }

                if (marker.IsMarker(setup, source))
                {
                    _map[source] = target;
                    return true;
                }

                // Constructed generic
                if (sourceNamedType.IsGenericType && targetNamedType.IsGenericType)
                {
                    if (types.TypeEquals(sourceNamedType.ConstructUnboundGenericType(), targetNamedType.ConstructUnboundGenericType()))
                    {
                        _map[source] = target;
                        var sourceArgs = sourceNamedType.TypeArguments;
                        var targetArgs = targetNamedType.TypeArguments;
                        if (sourceArgs.Length == targetArgs.Length)
                        {
                            for (var i = 0; i < sourceArgs.Length; i++)
                            {
                                result &= TryBindCore(setup, sourceArgs[i], targetArgs[i]);
                                if (!result)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }

                break;
            }

            case IArrayTypeSymbol sourceArrayType when target is IArrayTypeSymbol targetArrayType:
                result &= result && TryBindCore(setup, sourceArrayType.ElementType, targetArrayType.ElementType);
                break;

            default:
                result &= result && types.TypeEquals(source.OriginalDefinition, target.OriginalDefinition);
                break;
        }

        if (!result)
        {
            return result;
        }

        foreach (var implementationInterfaceType in target.Interfaces)
        {
            if (!types.TypeEquals(source.OriginalDefinition, implementationInterfaceType.OriginalDefinition))
            {
                continue;
            }

            result &= TryBindCore(setup, source, implementationInterfaceType);
            if (!result)
            {
                break;
            }
        }

        foreach (var dependencyInterfaceType in source.Interfaces)
        {
            if (!types.TypeEquals(target.OriginalDefinition, dependencyInterfaceType.OriginalDefinition))
            {
                continue;
            }

            result &= TryBindCore(setup, dependencyInterfaceType, target);
            if (!result)
            {
                break;
            }
        }

        return result;
    }

    public ITypeSymbol Construct(MdSetup setup, ITypeSymbol type)
    {
        if (!marker.IsMarkerBased(setup, type))
        {
            return type;
        }

        if (_map.TryGetValue(type, out var newType))
        {
            return newType;
        }

        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                return type;

            case INamedTypeSymbol namedType:
            {
                var args = namedType.TypeArguments.Select(CreateConstruct);
                return namedType.OriginalDefinition.Construct(args.ToArray());
                ITypeSymbol CreateConstruct(ITypeSymbol typeArgument) => Construct(setup, typeArgument);
            }

            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var originalElementType = Construct(setup, arrayTypeSymbol.ElementType);
                if (!_map.TryGetValue(originalElementType, out var elementType))
                {
                    elementType = originalElementType;
                }

                return setup.SemanticModel.Compilation.CreateArrayTypeSymbol(elementType, arrayTypeSymbol.Rank);
            }

            default:
                return type;
        }
    }

    public ITypeSymbol ConstructReversed(ITypeSymbol type)
    {
        if (_reversedMap.Count == 0)
        {
            foreach (var item in _map)
            {
                _reversedMap[item.Value] = item.Key;
            }
        }

        if (_reversedMap.TryGetValue(type, out var result))
        {
            return result;
        }

        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: false }:
                return type;

            case INamedTypeSymbol namedType:
            {
                var args = namedType.TypeArguments.Select(ConstructReversed);
                var constructed = namedType.OriginalDefinition.Construct(args.ToArray());
                return constructed.WithNullableAnnotation(namedType.NullableAnnotation);
            }

            default:
                return type;
        }
    }

    private static bool IsNullableReferenceTypeCompatible(ITypeSymbol source, ITypeSymbol target) =>
        source is not { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
        || target is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated };
}
