// ReSharper disable InvertIf
// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
internal sealed class TypeConstructor : ITypeConstructor
{
    private readonly IMarker _marker;
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _map = new(SymbolEqualityComparer.Default);
    
    public TypeConstructor(IMarker marker) => _marker = marker;

    public bool TryBind(ITypeSymbol source, ITypeSymbol target)
    {
        if (_marker.IsMarker(source))
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

                if (_marker.IsMarker(source))
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
                                result &= TryBind(sourceArgs[i], targetArgs[i]);
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
                result &= result && TryBind(sourceArrayType.ElementType, targetArrayType.ElementType);
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
            
            result &= TryBind(source, implementationInterfaceType);
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
            
            result &= TryBind(dependencyInterfaceType, target);
            if (!result)
            {
                break;
            }
        }

        return result;
    }

    public ITypeSymbol Construct(Compilation compilation, ITypeSymbol type)
    {
        if (!_marker.IsMarkerBased(type))
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
                ITypeSymbol CreateConstruct(ITypeSymbol typeArgument) => Construct(compilation, typeArgument);
            }

            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var originalElementType = Construct(compilation, arrayTypeSymbol.ElementType);
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
}