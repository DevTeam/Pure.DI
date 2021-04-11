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
        private readonly ResolverMetadata _metadata;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<ITypesMap> _typesMapFactory;
        private readonly Func<IObjectBuilder> _constructorObjectBuilder;
        private readonly Func<IObjectBuilder> _factoryObjectBuilder;
        private readonly Func<IObjectBuilder> _arrayObjectBuilder;
        private readonly Dictionary<Key, Binding<ITypeSymbol>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();
        private readonly HashSet<INamedTypeSymbol> _specialTypes = new(SymbolEqualityComparer.Default);

        public TypeResolver(
            SemanticModel semanticModel,
            ResolverMetadata metadata,
            IDiagnostic diagnostic,
            Func<ITypesMap> typesMapFactory,
            [Tag(Tags.AutowiringBuilder)] Func<IObjectBuilder> constructorObjectBuilder,
            [Tag(Tags.FactoryBuilder)] Func<IObjectBuilder> factoryObjectBuilder,
            [Tag(Tags.ArrayBuilder)] Func<IObjectBuilder> arrayObjectBuilder)
        {
            _semanticModel = semanticModel;
            _metadata = metadata;
            _diagnostic = diagnostic;
            _typesMapFactory = typesMapFactory;
            _constructorObjectBuilder = constructorObjectBuilder;
            _factoryObjectBuilder = factoryObjectBuilder;
            _arrayObjectBuilder = arrayObjectBuilder;

            var specialTypes= 
                from specialType in Enum.GetValues(typeof(SpecialType)).OfType<SpecialType>()
                where specialType != SpecialType.None
                select _semanticModel.Compilation.GetSpecialType(specialType);

            foreach (var specialType in specialTypes)
            {
                _specialTypes.Add(specialType);
            }

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

        public TypeDescription Resolve(ITypeSymbol contractTypeSymbol, ExpressionSyntax? tag, bool anyTag = false, bool suppressWarnings = false)
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
                            return new TypeDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder(), typesMap, _semanticModel);
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
                            return new TypeDescription(implementationEntry.Metadata, implementationType, tag, _constructorObjectBuilder(), typesMap, _semanticModel);
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
                            return new TypeDescription(factory.Metadata, contractType, tag, _factoryObjectBuilder(), typesMap, _semanticModel);
                        }

                        return new TypeDescription(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorObjectBuilder(), typesMap, _semanticModel);
                    }
                }

                if (
                    !contractTypeSymbol.IsAbstract
                    && !_specialTypes.Contains(contractTypeSymbol)
                    && contractTypeSymbol.IsValidTypeToResolve(_semanticModel))
                {
                    var typesMap = _typesMapFactory();
                    var newBinding = new BindingMetadata
                    {
                        ImplementationType = contractType
                    };

                    return new TypeDescription(newBinding, contractTypeSymbol, null, _constructorObjectBuilder(), typesMap, _semanticModel);
                }
            }

            if (contractTypeSymbol is IArrayTypeSymbol arrayType)
            {
                return new TypeDescription(new BindingMetadata(), arrayType, null, _arrayObjectBuilder(), _typesMapFactory(), _semanticModel);
            }

            if (!suppressWarnings)
            {
                if (_metadata.Fallback.Any())
                {
                    _diagnostic.Warning(Diagnostics.CannotResolveDependencyWarning, $"Cannot resolve a dependency {contractTypeSymbol}({tag}). Will use a fallback resolve method.");
                }
                else
                {
                    _diagnostic.Error(Diagnostics.CannotResolveDependencyError, $"Cannot resolve a dependency {contractTypeSymbol}({tag}). Please add an appropriate binding or a fallback resolve method.");
                }
            }

            return new TypeDescription(new BindingMetadata(), contractTypeSymbol, null, _constructorObjectBuilder(), _typesMapFactory(), _semanticModel, false);
        }

        public IEnumerable<TypeDescription> Resolve(ITypeSymbol contractTypeSymbol)
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
