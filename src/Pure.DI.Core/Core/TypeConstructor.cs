// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
internal sealed class TypeConstructor : ITypeConstructor
{
    private readonly IMarker _marker;
    private readonly Dictionary<ITypeSymbol,ITypeSymbol> _map = new(SymbolEqualityComparer.Default);
    
    public TypeConstructor(IMarker marker) => _marker = marker;

    public void Bind(ITypeSymbol source, ITypeSymbol target)
    {
        switch (source)
        {
            case INamedTypeSymbol sourceNamedType when target is INamedTypeSymbol targetNamedType:
            {
                if (!sourceNamedType.IsGenericType && SymbolEqualityComparer.Default.Equals(source, target))
                {
                    return;
                }

                if (_map.ContainsKey(source))
                {
                    return;
                }

                if (_marker.IsMarker(source))
                {
                    _map[source] = target;
                    return;
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
                                Bind(sourceArgs[i], targetArgs[i]);
                            }
                        }

                        return;
                    }
                }

                break;
            }
            
            case IArrayTypeSymbol sourceArrayType when target is IArrayTypeSymbol targetArrayType:
                Bind(sourceArrayType.ElementType, targetArrayType.ElementType);
                break;
        }

        foreach (var implementationInterfaceType in target.Interfaces)
        {
            Bind(source, implementationInterfaceType);
        }

        foreach (var dependencyInterfaceType in source.Interfaces)
        {
            Bind(dependencyInterfaceType, target);
        }
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
                var args = namedType.TypeArguments.Select(i => Construct(compilation, i));
                return namedType.OriginalDefinition.Construct(args.ToArray());
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