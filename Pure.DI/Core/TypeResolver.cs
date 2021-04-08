namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeResolver : ITypeResolver
    {
        private readonly SemanticModel _semanticModel;
        private readonly Func<ITypesMap> _typesMapFactory;
        private readonly IObjectBuilder _constructorObjectBuilder;
        private readonly IObjectBuilder _factoryObjectBuilder;
        private readonly IObjectBuilder _arrayObjectBuilder;
        private readonly Dictionary<Key, Binding<ITypeSymbol>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();

        public TypeResolver(
            SemanticModel semanticModel,
            ResolverMetadata metadata,
            Func<ITypesMap> typesMapFactory,
            [Tag(Tags.AutowiringBuilder)] IObjectBuilder constructorObjectBuilder,
            [Tag(Tags.FactoryBuilder)] IObjectBuilder factoryObjectBuilder,
            [Tag(Tags.ArrayBuilder)] IObjectBuilder arrayObjectBuilder)
        {
            _semanticModel = semanticModel;
            _typesMapFactory = typesMapFactory;
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
                                  && bindingContractType is INamedTypeSymbol namedType
                                ? new Key(namedType.ConstructUnboundGenericType(), tag)
                                : new Key(bindingContractType, tag);

                        _map[key] = new Binding<ITypeSymbol>(binding, binding.ImplementationType);

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
                Binding<ITypeSymbol> implementationEntry;
                if (contractType.IsGenericType)
                {
                    var key = new Key(contractType.ConstructUnboundGenericType(), tag, anyTag);
                    if (_map.TryGetValue(key, out implementationEntry))
                    {
                        var typesMap = _typesMapFactory();
                        var hasTypesMap = typesMap.Setup(implementationEntry.Details, contractType);
                        if (_factories.TryGetValue(key, out var factory))
                        {
                            return new TypeResolveDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder, typesMap, _semanticModel);
                        }

                        if (hasTypesMap && implementationEntry.Metadata.ImplementationType != null)
                        {
                            var constructedContractType = typesMap.ConstructType(implementationEntry.Details);
                            var implementationType = typesMap.ConstructType(implementationEntry.Metadata.ImplementationType);
                            var binding = new BindingMetadata
                            {
                                ImplementationType = implementationType,
                                Lifetime = implementationEntry.Metadata.Lifetime,
                                Location = implementationEntry.Metadata.Location
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
                        var typesMap = _typesMapFactory();
                        typesMap.Setup(implementationEntry.Details, contractType);
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
                return new TypeResolveDescription(new BindingMetadata(), arrayType, null, _arrayObjectBuilder, _typesMapFactory(), _semanticModel);
            }

            return new TypeResolveDescription(new BindingMetadata(), contractTypeSymbol, null, _constructorObjectBuilder, _typesMapFactory(), _semanticModel, false);
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
            private readonly string _tagStr;

            public Key(ITypeSymbol contractType, ExpressionSyntax? tag, bool anyTag = false)
            {
                ContractType = contractType;
                Tag = tag;
                _tagStr = tag?.ToString() ?? string.Empty;
                _anyTag = anyTag;
            }

            private bool Equals(Key other) =>
                ContractType.Equals(other.ContractType, SymbolEqualityComparer.Default)
                && (_anyTag || other._anyTag || Equals(_tagStr, other._tagStr));

            public override bool Equals(object obj) =>
                obj is Key other
                && Equals(other);

            public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(ContractType);

            public override string ToString()
            {
                var tag = _anyTag ? "Any" : Tag?.ToString();
                return $"{ContractType}({tag})";
            }
        }
    }
}
