namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class TypeResolver : ITypeResolver
    {
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<INamedTypeSymbol, INamedTypeSymbol> _map = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.IncludeNullability);
        private readonly Dictionary<INamedTypeSymbol, GenericTypeInfo> _genericMap = new Dictionary<INamedTypeSymbol, GenericTypeInfo>(SymbolEqualityComparer.IncludeNullability);
        private readonly HashSet<BindingMetadata> _additionalBindings = new HashSet<BindingMetadata>();

        public TypeResolver(ResolverMetadata metadata, SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            foreach (var binding in metadata.Bindings)
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    if (contractType.IsComposedGenericTypeMarker(semanticModel))
                    {
                        _genericMap[contractType.ConstructUnboundGenericType()] = new GenericTypeInfo(contractType, binding);
                    }
                    else
                    {
                        _map[contractType] = binding.ImplementationType;
                    }
                }
            }
        }

        public IReadOnlyCollection<BindingMetadata> AdditionalBindings => _additionalBindings;

        public INamedTypeSymbol Resolve(INamedTypeSymbol typeSymbol)
        {
            if (_map.TryGetValue(typeSymbol, out var implementationType))
            {
                var typeMap = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>();
                CreateMap(typeSymbol, implementationType, typeMap, _semanticModel);
                return implementationType;
            }

            if (typeSymbol.IsGenericType)
            {
                if(_genericMap.TryGetValue(typeSymbol.ConstructUnboundGenericType(), out var genericTypeInfo))
                {
                    var typesMap = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>();
                    CreateMap(genericTypeInfo.ContractType, typeSymbol, typesMap, _semanticModel);
                    if (typesMap.Count > 0)
                    { 
                        var contractType = ConstructType(genericTypeInfo.ContractType, typesMap);
                        implementationType = ConstructType(genericTypeInfo.Binding.ImplementationType, typesMap);
                        var binding = new BindingMetadata
                        {
                            ImplementationType = implementationType,
                            Lifetime = genericTypeInfo.Binding.Lifetime
                        };

                        foreach (var tag in genericTypeInfo.Binding.Tags)
                        {
                            binding.Tags.Add(tag);
                        }

                        binding.ContractTypes.Add(contractType);
                        _additionalBindings.Add(binding);
                        return implementationType;
                    }
                }
            }

            return typeSymbol;
        }

        private void CreateMap(INamedTypeSymbol type, INamedTypeSymbol targetType, IDictionary<INamedTypeSymbol, INamedTypeSymbol> typesMap, SemanticModel semanticModel)
        {
            if (type.Equals(targetType, SymbolEqualityComparer.IncludeNullability))
            {
                return;
            }

            if (typesMap.ContainsKey(type))
            {
                return;
            }

            if (type.IsGenericTypeMarker(semanticModel))
            {
                typesMap[type] = targetType;
                return;
            }

            // Constructed generic
            if (targetType.IsGenericType)
            {
                if (type.ConstructUnboundGenericType().Equals(targetType.ConstructUnboundGenericType(), SymbolEqualityComparer.IncludeNullability))
                {
                    typesMap[type] = targetType;
                    var typeArgs = type.TypeArguments;
                    var targetTypeArgs = targetType.TypeArguments;
                    for (var i = 0; i < typeArgs.Length; i++)
                    {
                        if (
                            typeArgs[i] is INamedTypeSymbol typeArg
                            && targetTypeArgs[i] is INamedTypeSymbol targetTypeArg)
                        {
                            CreateMap(typeArg, targetTypeArg, typesMap, semanticModel);
                        }
                    }

                    return;
                }

                foreach (var implementedInterface in targetType.Interfaces)
                {
                    CreateMap(type, implementedInterface, typesMap, semanticModel);
                }

                foreach (var implementedInterface in type.Interfaces)
                {
                    CreateMap(implementedInterface, targetType, typesMap, semanticModel);
                }

                return;
            }

            // Array
            /*if (targetType.IsArray())
            {
                Map(type.GetElementType(), targetType.GetElementType(), typesMap);
                typesMap[type.Type] = targetType.Type;
            }*/
        }

        private INamedTypeSymbol ConstructType(INamedTypeSymbol type, IDictionary<INamedTypeSymbol, INamedTypeSymbol> typesMap)
        {
            if (typesMap.TryGetValue(type, out var newType))
            {
                return newType;
            }

            if (!type.IsGenericType)
            {
                return type;
            }

            var args = new List<ITypeSymbol>();
            foreach (var arg in type.TypeArguments)
            {
                var typeArgument = arg;
                if (typeArgument is INamedTypeSymbol namedTypeArgument)
                {
                    typeArgument = ConstructType(namedTypeArgument, typesMap);
                }

                args.Add(typeArgument);
            }

            return type.Construct(_semanticModel, args.ToArray());
        }

        private readonly struct GenericTypeInfo
        {
            public readonly INamedTypeSymbol ContractType;
            public readonly BindingMetadata Binding;

            public GenericTypeInfo(INamedTypeSymbol contractType, BindingMetadata binding)
            {
                ContractType = contractType;
                Binding = binding;
            }
        }
    }
}
