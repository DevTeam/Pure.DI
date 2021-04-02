namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    internal class TypeResolver : ITypeResolver
    {
        private readonly SemanticModel _semanticModel;
        private readonly IObjectBuilder _constructorObjectBuilder;
        private readonly IObjectBuilder _factoryObjectBuilder;
        private readonly Dictionary<Key, Binding<INamedTypeSymbol>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();

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
                if (binding.ImplementationType == null)
                {
                    continue;
                }
                
                foreach (var bindingContractType in binding.ContractTypes)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null))
                    {
                        var key = bindingContractType.IsComposedGenericTypeMarker(semanticModel) 
                            ? new Key(bindingContractType.ConstructUnboundGenericType(), tag)
                            : new Key(bindingContractType, tag);

                        _map[key] = new Binding<INamedTypeSymbol>(binding, binding.ImplementationType);

                        if (binding.Factory != null)
                        {
                            _factories[key] = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                        }
                    }
                }
            }
        }

        public TypeResolveDescription Resolve(INamedTypeSymbol contractType, ExpressionSyntax? tag, bool anyTag = false)
        {
            Binding<INamedTypeSymbol> implementationEntry;
            if (contractType.IsGenericType)
            {
                var key = new Key(contractType.ConstructUnboundGenericType(), tag, anyTag);
                if (_map.TryGetValue(key, out implementationEntry))
                {
                    var typesMap = new TypesMap(implementationEntry.Details, contractType, _semanticModel);
                    if (_factories.TryGetValue(key, out var factory))
                    {
                        return new TypeResolveDescription(factory.Metadata, contractType, _factoryObjectBuilder, typesMap, _semanticModel);
                    }

                    if (typesMap.Count > 0 && implementationEntry.Metadata.ImplementationType != null)
                    { 
                        var constructedContractType = typesMap.ConstructType(implementationEntry.Details);
                        var implementationType = typesMap.ConstructType(implementationEntry.Metadata.ImplementationType);
                        var binding = new BindingMetadata
                        {
                            ImplementationType = implementationType,
                            Lifetime = implementationEntry.Metadata.Lifetime
                        };

                        if (tag != null)
                        {
                            binding.Tags.Add(tag);
                        }

                        binding.ContractTypes.Add(constructedContractType);
                        return new TypeResolveDescription(implementationEntry.Metadata, implementationType, _constructorObjectBuilder, typesMap, _semanticModel);
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
                        return new TypeResolveDescription(factory.Metadata, contractType, _factoryObjectBuilder, typesMap, _semanticModel);
                    }

                    return new TypeResolveDescription(implementationEntry.Metadata, implementationEntry.Details, _constructorObjectBuilder, typesMap, _semanticModel);
                }
            }

            return new TypeResolveDescription(new BindingMetadata(), contractType, _constructorObjectBuilder, new TypesMap(_semanticModel), _semanticModel, false);
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
            private readonly INamedTypeSymbol _contractType;
            private readonly ExpressionSyntax? _tag;
            private readonly bool _anyTag;

            public Key(INamedTypeSymbol contractType, ExpressionSyntax? tag, bool anyTag = false)
            {
                _contractType = contractType;
                _tag = tag;
                _anyTag = anyTag;
            }

            private bool Equals(Key other) =>
                _contractType.Equals(other._contractType, SymbolEqualityComparer.Default)
                && (_anyTag || other._anyTag || Equals(_tag, other._tag));

            public override bool Equals(object obj) =>
                obj is Key other
                && (_anyTag || other._anyTag || Equals(other));

            public override int GetHashCode()
            {
                return SymbolEqualityComparer.Default.GetHashCode(_contractType);
            }
        }
    }
}
