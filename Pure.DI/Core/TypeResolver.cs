namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeResolver : ITypeResolver
    {
        private readonly SemanticModel _semanticModel;
        private readonly IObjectBuilder _constructorObjectBuilder;
        private readonly IObjectBuilder _factoryObjectBuilder;
        private readonly Dictionary<Key, Binding<INamedTypeSymbol>> _map = new Dictionary<Key, Binding<INamedTypeSymbol>>();
        private readonly Dictionary<Key, Binding<INamedTypeSymbol>> _genericMap = new Dictionary<Key, Binding<INamedTypeSymbol>>();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>>();

        public TypeResolver(
            ResolverMetadata metadata,
            SemanticModel semanticModel,
            IObjectBuilder constructorObjectBuilder,
            IObjectBuilder factoryObjectBuilder)
        {
            _semanticModel = semanticModel;
            _constructorObjectBuilder = constructorObjectBuilder;
            _factoryObjectBuilder = factoryObjectBuilder;
            foreach (var binding in metadata.Bindings)
            {
                foreach (var bindingContractType in binding.ContractTypes)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty(null))
                    {
                        Key key;
                        if (bindingContractType.IsComposedGenericTypeMarker(semanticModel))
                        {
                            key = new Key(bindingContractType.ConstructUnboundGenericType(), tag);
                            _genericMap[key] = new Binding<INamedTypeSymbol>(binding, bindingContractType);
                        }
                        else
                        {
                            key = new Key(bindingContractType, tag);
                            _map[key] = new Binding<INamedTypeSymbol>(binding, binding.ImplementationType);
                        }

                        if (binding.Factory != null)
                        {
                            _factories[key] = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                        }
                    }
                }
            }
        }

        public TypeResolveDescription Resolve(INamedTypeSymbol contractType, ExpressionSyntax tag)
        {
            var key = new Key(contractType, tag);
            if (_factories.TryGetValue(key, out var factory))
            {
                return new TypeResolveDescription(factory.Metadata, contractType, _factoryObjectBuilder);
            }

            if (_map.TryGetValue(key, out var implementationEntry))
            {
                var typeMap = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>();
                CreateMap(contractType, implementationEntry.Details, typeMap, _semanticModel);
                return new TypeResolveDescription(implementationEntry.Metadata, implementationEntry.Details, _constructorObjectBuilder);
            }

            if (contractType.IsGenericType)
            {
                if (_genericMap.TryGetValue(new Key(contractType.ConstructUnboundGenericType(), tag), out implementationEntry))
                {
                    var typesMap = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>();
                    CreateMap(implementationEntry.Details, contractType, typesMap, _semanticModel);
                    if (typesMap.Count > 0)
                    { 
                        var constructedContractType = ConstructType(implementationEntry.Details, typesMap);
                        var implementationType = ConstructType(implementationEntry.Metadata.ImplementationType, typesMap);
                        var binding = new BindingMetadata
                        {
                            ImplementationType = implementationType,
                            Lifetime = implementationEntry.Metadata.Lifetime
                        };

                        binding.Tags.Add(tag);
                        binding.ContractTypes.Add(constructedContractType);
                        return new TypeResolveDescription(implementationEntry.Metadata, implementationType, _constructorObjectBuilder);
                    }
                }
            }

            return new TypeResolveDescription(new BindingMetadata(), contractType, _constructorObjectBuilder);
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

        private readonly struct Binding<T>
        {
            public readonly BindingMetadata Metadata;
            public readonly T Details;

            public Binding(BindingMetadata metadata, T details)
            {
                Metadata = metadata;
                Details = details;
            }
        }

        private readonly struct Key
        {
            public readonly INamedTypeSymbol ContractType;
            public readonly ExpressionSyntax Tag;

            public Key(INamedTypeSymbol contractType, ExpressionSyntax tag)
            {
                ContractType = contractType;
                Tag = tag;
            }

            public bool Equals(Key other) =>
                ContractType.Equals(other.ContractType, SymbolEqualityComparer.IncludeNullability)
                && Equals(Tag, other.Tag);

            public override bool Equals(object obj) =>
                obj is Key other
                && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ContractType != null ? SymbolEqualityComparer.IncludeNullability.GetHashCode(ContractType) : 0) * 397) ^ (Tag != null ? Tag.GetHashCode() : 0);
                }
            }
        }
    }
}
