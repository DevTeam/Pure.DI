// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypesMap : ITypesMap
    {
        private readonly Dictionary<SemanticType, SemanticType> _map = new(SemanticTypeEqualityComparer.Default);

        public bool Setup(SemanticType dependency, SemanticType implementation)
        {
            _map.Clear();
            CreateMap(dependency, implementation);
            return _map.Count > 0;
        }

        private void CreateMap(SemanticType dependency, SemanticType implementation)
        {
            if (
                dependency.Type is INamedTypeSymbol dependencyNamedType
                && implementation.Type is INamedTypeSymbol implementationNamedType)
            {
                if (!dependencyNamedType.IsGenericType && dependency.Equals(implementation))
                {
                    return;
                }

                if (_map.ContainsKey(dependency))
                {
                    return;
                }

                if (dependency.IsGenericTypeMarker)
                {
                    _map[dependency] = implementation;
                    return;
                }

                // Constructed generic
                if (dependencyNamedType.IsGenericType && implementationNamedType.IsGenericType)
                {
                    if (dependencyNamedType.ConstructUnboundGenericType().Equals(implementationNamedType.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                    {
                        _map[dependency] = implementation;
                        var dependencyArgs = dependencyNamedType.TypeArguments;
                        var implementationArgs = implementationNamedType.TypeArguments;
                        if (dependencyArgs.Length == implementationArgs.Length)
                        {
                            for (var i = 0; i < dependencyArgs.Length; i++)
                            {
                                CreateMap(new SemanticType(dependencyArgs[i], dependency), new SemanticType(implementationArgs[i], implementation));
                            }
                        }

                        return;
                    }
                }
            }

            if (
                dependency.Type is IArrayTypeSymbol dependencyArrayType
                && implementation.Type is IArrayTypeSymbol implementationArrayType)
            {
                var dependencyArray = new SemanticType(dependencyArrayType.ElementType, dependency);
                var implementationArray = new SemanticType(implementationArrayType.ElementType, implementation);
                _map[dependencyArray] = implementationArray;
                CreateMap(new SemanticType(dependencyArrayType.ElementType, dependencyArray), new SemanticType(implementationArrayType.ElementType, implementationArray));
            }

            foreach (var implementationInterfaceType in implementation.Type.Interfaces)
            {
                CreateMap(dependency, new SemanticType(implementationInterfaceType, implementation));
            }

            foreach (var dependencyInterfaceType in dependency.Type.Interfaces)
            {
                CreateMap(new SemanticType(dependencyInterfaceType, dependency), implementation);
            }
        }

        public SemanticType ConstructType(SemanticType type)
        {
            if (!type.IsComposedGenericTypeMarker)
            {
                return type;
            }

            if (_map.TryGetValue(type, out var newType))
            {
                return newType;
            }

            switch (type.Type)
            {
                case INamedTypeSymbol {IsGenericType: false}:
                    return type;

                case INamedTypeSymbol namedType:
                {
                    var args = namedType.TypeArguments.Select(arg => ConstructType(new SemanticType(arg, type)));
                    return new SemanticType(namedType, type).Construct(args.ToArray());
                }

                case IArrayTypeSymbol arrayTypeSymbol:
                {
                    var originalElementType = ConstructType(new SemanticType(arrayTypeSymbol.ElementType, type));
                    if (!_map.TryGetValue(originalElementType, out var elementType))
                    {
                        elementType = originalElementType;
                    }

                    return new SemanticType(type.SemanticModel.Compilation.CreateArrayTypeSymbol(elementType.Type, arrayTypeSymbol.Rank), elementType);
                }

                default:
                    return type;
            }
        }
    }
}
