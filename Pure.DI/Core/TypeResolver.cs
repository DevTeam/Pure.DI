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
        private readonly IObjectBuilder _arrayObjectBuilder;
        private readonly Dictionary<Key, Binding<INamedTypeSymbol>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();

        public TypeResolver(
            ResolverMetadata metadata,
            SemanticModel semanticModel,
            IObjectBuilder constructorObjectBuilder,
            IObjectBuilder factoryObjectBuilder,
            IObjectBuilder arrayObjectBuilder)
        {
            _semanticModel = semanticModel;
            _constructorObjectBuilder = constructorObjectBuilder;
            _factoryObjectBuilder = factoryObjectBuilder;
            _arrayObjectBuilder = arrayObjectBuilder;
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

        public TypeResolveDescription Resolve(ITypeSymbol contractTypeSymbol, ExpressionSyntax? tag, bool anyTag = false)
        {
            if (contractTypeSymbol is INamedTypeSymbol contractType)
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
                            return new TypeResolveDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder, typesMap, _semanticModel);
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
                            return new TypeResolveDescription(implementationEntry.Metadata, implementationType, tag, _constructorObjectBuilder, typesMap, _semanticModel);
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
                            return new TypeResolveDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder, typesMap, _semanticModel);
                        }

                        return new TypeResolveDescription(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorObjectBuilder, typesMap, _semanticModel);
                    }
                }
            }

            if (contractTypeSymbol is IArrayTypeSymbol arrayType)
            {
                return new TypeResolveDescription(new BindingMetadata(), arrayType, null, _arrayObjectBuilder, new TypesMap(_semanticModel), _semanticModel);
            }

            return new TypeResolveDescription(new BindingMetadata(), contractTypeSymbol, null, _constructorObjectBuilder, new TypesMap(_semanticModel), _semanticModel, false);
        }

        public IEnumerable<TypeResolveDescription> Resolve(ITypeSymbol contractTypeSymbol)
        {
            var keyToFind = new Key(contractTypeSymbol, null, true);
            var registeredKeys =
                from key in _factories.Keys.Concat(_map.Keys).Distinct()
                where keyToFind.Equals(key)
                select key;

            foreach (var registeredKey in registeredKeys)
            {
                yield return Resolve(registeredKey.ContractType, registeredKey.Tag);
            }
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
            public readonly ITypeSymbol ContractType;
            public readonly ExpressionSyntax? Tag;
            private readonly bool _anyTag;

            public Key(ITypeSymbol contractType, ExpressionSyntax? tag, bool anyTag = false)
            {
                ContractType = contractType;
                Tag = tag;
                _anyTag = anyTag;
            }

            private bool Equals(Key other) =>
                ContractType.Equals(other.ContractType, SymbolEqualityComparer.Default)
                && (_anyTag || other._anyTag || Equals(Tag, other.Tag));

            public override bool Equals(object obj) =>
                obj is Key other
                && Equals(other);

            public override int GetHashCode()
            {
                return SymbolEqualityComparer.Default.GetHashCode(ContractType);
            }
        }
    }
}
