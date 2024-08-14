// ReSharper disable InvertIf
// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
internal sealed class TypeConstructor(IMarker marker) : ITypeConstructor
{
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _map = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _reversedMap = new(SymbolEqualityComparer.Default);

    public bool TryBind(MdSetup setup, ITypeSymbol source, ITypeSymbol target)
    {
        _reversedMap.Clear();
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
                if (!SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, target.OriginalDefinition))
                {
                    return false;
                }

                if (!sourceNamedType.IsGenericType)
                {
                    return SymbolEqualityComparer.Default.Equals(source, target);
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
                    if (sourceNamedType.ConstructUnboundGenericType().Equals(targetNamedType.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                    {
                        _map[source] = target;
                        var sourceArgs = sourceNamedType.TypeArguments;
                        var targetArgs = targetNamedType.TypeArguments;
                        if (sourceArgs.Length == targetArgs.Length)
                        {
                            for (var i = 0; i < sourceArgs.Length; i++)
                            {
                                result &= TryBind(setup, sourceArgs[i], targetArgs[i]);
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
                result &= result && TryBind(setup, sourceArrayType.ElementType, targetArrayType.ElementType);
                break;
            
            default:
                result &= result && SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, target.OriginalDefinition);
                break;
        }

        if (!result)
        {
            return result;
        }

        foreach (var implementationInterfaceType in target.Interfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, implementationInterfaceType.OriginalDefinition))
            {
                continue;
            }
            
            result &= TryBind(setup, source, implementationInterfaceType);
            if (!result)
            {
                break;
            }
        }

        foreach (var dependencyInterfaceType in source.Interfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(target.OriginalDefinition, dependencyInterfaceType.OriginalDefinition))
            {
                continue;
            }
            
            result &= TryBind(setup, dependencyInterfaceType, target);
            if (!result)
            {
                break;
            }
        }

        return result;
    }

    public ITypeSymbol Construct(MdSetup setup, Compilation compilation, ITypeSymbol type)
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
                ITypeSymbol CreateConstruct(ITypeSymbol typeArgument) => Construct(setup, compilation, typeArgument);
            }

            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var originalElementType = Construct(setup, compilation, arrayTypeSymbol.ElementType);
                if (!_map.TryGetValue(originalElementType, out var elementType))
                {
                    elementType = originalElementType;
                }

                return compilation.CreateArrayTypeSymbol(elementType, arrayTypeSymbol.Rank);
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

        return _reversedMap.TryGetValue(type, out var result) ? result : type;
    }
}