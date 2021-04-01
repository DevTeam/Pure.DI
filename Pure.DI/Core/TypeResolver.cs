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
                        }
                        else
                        {
                            key = new Key(bindingContractType, tag);
                            
                        }

                        _map[key] = new Binding<INamedTypeSymbol>(binding, binding.ImplementationType);

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
            Binding<INamedTypeSymbol> implementationEntry;
            if (contractType.IsGenericType)
            {
                var key = new Key(contractType.ConstructUnboundGenericType(), tag);
                if (_map.TryGetValue(key, out implementationEntry))
                {
                    var typesMap = new TypesMap(implementationEntry.Details, contractType, _semanticModel);
                    if (_factories.TryGetValue(key, out var factory))
                    {
                        return new TypeResolveDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder, typesMap, _semanticModel, this);
                    }

                    if (typesMap.Count > 0)
                    { 
                        var constructedContractType = typesMap.ConstructType(implementationEntry.Details);
                        var implementationType = typesMap.ConstructType(implementationEntry.Metadata.ImplementationType);
                        var binding = new BindingMetadata
                        {
                            ImplementationType = implementationType,
                            Lifetime = implementationEntry.Metadata.Lifetime
                        };

                        binding.Tags.Add(tag);
                        binding.ContractTypes.Add(constructedContractType);
                        return new TypeResolveDescription(implementationEntry.Metadata, implementationType, tag, _constructorObjectBuilder, typesMap, _semanticModel, this);
                    }
                }
            }
            else
            {
                var key = new Key(contractType, tag);
                if (_map.TryGetValue(key, out implementationEntry))
                {
                    var typesMap = new TypesMap(implementationEntry.Details, contractType, _semanticModel);
                    if (_factories.TryGetValue(key, out var factory))
                    {
                        return new TypeResolveDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder, typesMap, _semanticModel, this);
                    }

                    return new TypeResolveDescription(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorObjectBuilder, typesMap, _semanticModel, this);
                }
            }

            return new TypeResolveDescription(new BindingMetadata(), contractType, tag, _constructorObjectBuilder, new TypesMap(_semanticModel), _semanticModel, this, false);
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
