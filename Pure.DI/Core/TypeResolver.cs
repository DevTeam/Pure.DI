// ReSharper disable InvertIf
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
        private readonly ResolverMetadata _metadata;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<ITypesMap> _typesMapFactory;
        private readonly Func<IObjectBuilder> _constructorObjectBuilder;
        private readonly Func<IObjectBuilder> _factoryObjectBuilder;
        private readonly Func<IObjectBuilder> _arrayObjectBuilder;
        private readonly Dictionary<Key, Binding<SemanticType>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();
        private readonly HashSet<SemanticType> _specialTypes = new(SemanticTypeEqualityComparer.Default);

        public TypeResolver(
            ResolverMetadata metadata,
            IDiagnostic diagnostic,
            Func<ITypesMap> typesMapFactory,
            [Tag(Tags.AutowiringBuilder)] Func<IObjectBuilder> constructorObjectBuilder,
            [Tag(Tags.FactoryBuilder)] Func<IObjectBuilder> factoryObjectBuilder,
            [Tag(Tags.ArrayBuilder)] Func<IObjectBuilder> arrayObjectBuilder)
        {
            _metadata = metadata;
            _diagnostic = diagnostic;
            _typesMapFactory = typesMapFactory;
            _constructorObjectBuilder = constructorObjectBuilder;
            _factoryObjectBuilder = factoryObjectBuilder;
            _arrayObjectBuilder = arrayObjectBuilder;

            foreach (var binding in metadata.Bindings)
            {
                if (binding.Implementation == null)
                {
                    continue;
                }
                
                foreach (var dependency in binding.Dependencies)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null))
                    {
                        var key = dependency.IsComposedGenericTypeMarker
                                  && dependency.Type is INamedTypeSymbol namedType
                                ? new Key(new SemanticType(namedType.ConstructUnboundGenericType(), dependency.SemanticModel), tag)
                                : new Key(dependency, tag);

                        if (_map.TryGetValue(key, out _))
                        {
                            _diagnostic.Warning(Diagnostics.BindingIsAlreadyExist, $"{key} binding was exist and will be overridden by a new one.");
                        }

                        _map[key] = new Binding<SemanticType>(binding, binding.Implementation);

                        if (binding.Factory != null)
                        {
                            _factories[key] = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                        }
                    }
                }
            }
        }

        public Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, bool anyTag = false, bool suppressWarnings = false)
        {
            switch (dependency.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    Binding<SemanticType> implementationEntry;
                    if (namedType.IsGenericType)
                    {
                        var keys = new[]
                        {
                            new Key(dependency, tag, anyTag),
                            new Key(new SemanticType(namedType.ConstructUnboundGenericType(), dependency), tag, anyTag)
                        };

                        foreach (var key in keys)
                        {

                            if (_map.TryGetValue(key, out implementationEntry))
                            {
                                var typesMap = _typesMapFactory();
                                var hasTypesMap = typesMap.Setup(implementationEntry.Details, dependency);
                                if (_factories.TryGetValue(key, out var factory))
                                {
                                    return new Dependency(factory.Metadata, dependency, tag, _factoryObjectBuilder(), typesMap);
                                }

                                if (hasTypesMap && implementationEntry.Metadata.Implementation != null)
                                {
                                    var constructedContractType = typesMap.ConstructType(implementationEntry.Details);
                                    var implementationType = typesMap.ConstructType(implementationEntry.Metadata.Implementation);
                                    var binding = new BindingMetadata
                                    {
                                        Implementation = implementationType,
                                        Lifetime = implementationEntry.Metadata.Lifetime,
                                        Location = implementationEntry.Metadata.Location
                                    };

                                    if (tag != null)
                                    {
                                        binding.Tags.Add(tag);
                                    }

                                    binding.Dependencies.Add(constructedContractType);
                                    return new Dependency(implementationEntry.Metadata, implementationType, tag, _constructorObjectBuilder(), typesMap);
                                }
                            }
                        }
                    }
                    else
                    {
                        var key = new Key(dependency, tag);
                        if (_map.TryGetValue(key, out implementationEntry))
                        {
                            var typesMap = _typesMapFactory();
                            typesMap.Setup(implementationEntry.Details, dependency);
                            return _factories.TryGetValue(key, out var factory)
                                ? new Dependency(factory.Metadata, dependency, tag, _factoryObjectBuilder(), typesMap)
                                : new Dependency(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorObjectBuilder(), typesMap);
                        }
                    }

                    if (
                        !dependency.Type.IsAbstract
                        && !GetSpecialTypes(dependency.SemanticModel).Contains(dependency)
                        && dependency.IsValidTypeToResolve)
                    {
                        var typesMap = _typesMapFactory();
                        var newBinding = new BindingMetadata
                        {
                            Implementation = dependency
                        };

                        return new Dependency(newBinding, dependency, null, _constructorObjectBuilder(), typesMap);
                    }

                    break;
                }

                case IArrayTypeSymbol:
                    return new Dependency(new BindingMetadata(), dependency, null, _arrayObjectBuilder(), _typesMapFactory());
            }

            if (!suppressWarnings)
            {
                if (_metadata.Fallback.Any())
                {
                    _diagnostic.Warning(Diagnostics.CannotResolveDependencyWarning, $"Cannot resolve a dependency {dependency}({tag}). Will use a fallback resolve method.");
                }
                else
                {
                    _diagnostic.Error(Diagnostics.CannotResolveDependencyError, $"Cannot resolve a dependency {dependency}({tag}). Please add an appropriate binding or a fallback resolve method.");
                }
            }

            return new Dependency(new BindingMetadata(), dependency, null, _constructorObjectBuilder(), _typesMapFactory(), false);
        }

        public IEnumerable<Dependency> Resolve(SemanticType dependency)
        {
            var keyToFind = new Key(dependency, null, true);
            var registeredKeys =
                from key in _factories.Keys.Concat(_map.Keys).Distinct()
                where keyToFind.Equals(key)
                select key;

            foreach (var registeredKey in registeredKeys)
            {
                yield return Resolve(registeredKey.Dependency, registeredKey.Tag);
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

        private IEnumerable<SemanticType> GetSpecialTypes(SemanticModel semanticModel)
        {
            if (_specialTypes.Count > 0)
            {
                return _specialTypes;
            }

            var specialTypes =
                from specialType in Enum.GetValues(typeof(SpecialType)).OfType<SpecialType>()
                where specialType != SpecialType.None
                select semanticModel.Compilation.GetSpecialType(specialType);

            foreach (var specialType in specialTypes)
            {
                _specialTypes.Add(new SemanticType(specialType, semanticModel));
            }

            return _specialTypes;
        }

        private readonly struct Key
        {
            public readonly SemanticType Dependency;
            public readonly ExpressionSyntax? Tag;
            private readonly bool _anyTag;
            private readonly string _tagStr;

            public Key(SemanticType dependency, ExpressionSyntax? tag, bool anyTag = false)
            {
                Dependency = dependency;
                Tag = tag;
                _tagStr = tag?.ToString() ?? string.Empty;
                _anyTag = anyTag;
            }

            private bool Equals(Key other) =>
                Dependency.Equals(other.Dependency)
                && (_anyTag || other._anyTag || Equals(_tagStr, other._tagStr));

            public override bool Equals(object obj) =>
                obj is Key other
                && Equals(other);

            public override int GetHashCode() => Dependency.GetHashCode();

            public override string ToString()
            {
                var tag = _anyTag ? "Any" : Tag?.ToString();
                return $"{Dependency}({tag})";
            }
        }
    }
}
