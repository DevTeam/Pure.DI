// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeResolver : ITypeResolver
    {
        private readonly IDiagnostic _diagnostic;
        private readonly IBuildContext _buildContext;
        private readonly Func<ITypesMap> _typesMapFactory;
        private readonly ITracer _tracer;
        private readonly Func<IObjectBuilder> _constructorBuilder;
        private readonly Func<IObjectBuilder> _factoryBuilder;
        private readonly Func<IObjectBuilder> _arrayBuilder;
        private readonly Func<IObjectBuilder> _enumerableBuilder;
        private readonly Dictionary<Key, Binding<SemanticType>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();
        private readonly HashSet<SemanticType> _specialTypes = new(SemanticTypeEqualityComparer.Default);

        public TypeResolver(
            ResolverMetadata metadata,
            IDiagnostic diagnostic,
            IBuildContext buildContext,
            Func<ITypesMap> typesMapFactory,
            ITracer tracer,
            [Tag(Tags.AutowiringBuilder)] Func<IObjectBuilder> constructorBuilder,
            [Tag(Tags.FactoryBuilder)] Func<IObjectBuilder> factoryBuilder,
            [Tag(Tags.ArrayBuilder)] Func<IObjectBuilder> arrayBuilder,
            [Tag(Tags.EnumerableBuilder)] Func<IObjectBuilder> enumerableBuilder)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _typesMapFactory = typesMapFactory;
            _tracer = tracer;
            _constructorBuilder = constructorBuilder;
            _factoryBuilder = factoryBuilder;
            _arrayBuilder = arrayBuilder;
            _enumerableBuilder = enumerableBuilder;

            foreach (var binding in metadata.Bindings)
            {
                if (binding.Implementation == null)
                {
                    continue;
                }

                var dependencies = new HashSet<SemanticType>(binding.Dependencies);
                foreach (var dependency in dependencies)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null))
                    {
                        var semanticType = dependency.IsComposedGenericTypeMarker
                                  && dependency.Type is INamedTypeSymbol namedType
                                ? new SemanticType(namedType.ConstructUnboundGenericType(), dependency.SemanticModel)
                                : dependency;
                        
                        if (
                            !GetSpecialTypes(semanticType).Contains(semanticType)
                            && dependency.SemanticModel.Compilation.GetTypeByMetadataName(semanticType.Name) == null)
                        {
                            continue;
                        }

                        var key = new Key(semanticType, tag, binding.AnyTag);
                        if (_map.TryGetValue(key, out var prev))
                        {
                            _diagnostic.Information(Diagnostics.Information.BindingAlreadyExists, $"{prev.Metadata} exists and will be overridden by a new one {binding}.");
                        }

                        _map[key] = new Binding<SemanticType>(binding, binding.Implementation);

                        if (binding.Factory != null)
                        {
                            _factories[key] = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                        }

                        if (binding.Lifetime == Lifetime.Scoped || binding.Lifetime == Lifetime.ContainerSingleton)
                        {
                            var serviceProviderInstance = new SemanticType(dependency.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.ServiceProviderInstance`1")!, dependency.SemanticModel).Construct(dependency);
                            _buildContext.AddBinding(new BindingMetadata(binding, serviceProviderInstance, binding.Id));
                        }
                    }
                }
            }
        }

        public Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, ImmutableArray<Location> resolveLocations, bool anyTag = false, bool probe = false)
        {
            switch (dependency.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    Binding<SemanticType> implementationEntry;
                    if (namedType.IsGenericType)
                    {
                        var unboundDependency = new SemanticType(namedType.ConstructUnboundGenericType(), dependency);
                        var keys = new[]
                        {
                            new Key(dependency, tag, anyTag),
                            new Key(unboundDependency, tag, anyTag)
                        };

                        foreach (var key in keys)
                        {
                            if (_map.TryGetValue(key, out implementationEntry))
                            {
                                var typesMap = _typesMapFactory();
                                var hasTypesMap = typesMap.Setup(implementationEntry.Details, dependency);
                                var constructedImplementation = typesMap.ConstructType(implementationEntry.Details);
                                var constructedDependency = typesMap.ConstructType(dependency);
                                if (_factories.TryGetValue(key, out var factory))
                                {
                                   return new Dependency(factory.Metadata, dependency, tag, _factoryBuilder(), typesMap);
                                }

                                if (hasTypesMap && implementationEntry.Metadata.Implementation != null)
                                {
                                    var binding = new BindingMetadata
                                    {
                                        Implementation = constructedImplementation,
                                        Lifetime = implementationEntry.Metadata.Lifetime,
                                        Location = implementationEntry.Metadata.Location
                                    };

                                    if (tag != null)
                                    {
                                        binding.Tags.Add(tag);
                                    }

                                    binding.Dependencies.Add(constructedDependency);
                                    binding.Dependencies.Add(constructedImplementation);

                                    _buildContext.AddBinding(binding);
                                    return new Dependency(implementationEntry.Metadata, constructedImplementation, tag, _constructorBuilder(), typesMap);
                                }
                            }
                        }

                        if (unboundDependency.Equals(typeof(IEnumerable<>)))
                        {
                            return new Dependency(new BindingMetadata(dependency), dependency, null, _enumerableBuilder(), _typesMapFactory());
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
                                ? new Dependency(factory.Metadata, dependency, tag, _factoryBuilder(), typesMap)
                                : new Dependency(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorBuilder(), typesMap);
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
                            Implementation = dependency,
                            Lifetime = Lifetime.Transient,
                            Dependencies = { dependency }
                        };

                        _buildContext.AddBinding(newBinding);
                        return new Dependency(newBinding, dependency, tag, _constructorBuilder(), typesMap);
                    }

                    break;
                }

                case IArrayTypeSymbol:
                    return new Dependency(new BindingMetadata(dependency), dependency, null, _arrayBuilder(), _typesMapFactory());
            }

            if (!probe)
            {
                var hasFallback = false;
                var fallbackType = dependency.SemanticModel.Compilation.GetTypeByMetadataName(typeof(IFallback).ToString());
                if (fallbackType != null)
                {
                    var fallbackKey = new SemanticType(fallbackType, dependency.SemanticModel);
                    if (_map.ContainsKey(new Key(fallbackKey, null, true)))
                    {
                        hasFallback = true;
                    }
                }

                if (hasFallback)
                {
                    _diagnostic.Warning(Diagnostics.Warning.CannotResolveDependency, $"Cannot resolve a dependency of the type {GetDependencyName(dependency, tag)} for {_tracer}. Will use a fallback strategy.", resolveLocations.FirstOrDefault());
                }
                else
                {
                    var error = $"Cannot resolve a dependency of the type {GetDependencyName(dependency, tag)} for {_tracer}. Please add an appropriate binding, remove this dependency or rely on a fallback strategy.";
                    _diagnostic.Error(Diagnostics.Error.CannotResolveDependency, error, resolveLocations.FirstOrDefault());
                    throw new HandledException(error);
                }
            }

            return new Dependency(new BindingMetadata(dependency), dependency, null, _constructorBuilder(), _typesMapFactory(), false);
        }

        private static string GetDependencyName(SemanticType dependency, ExpressionSyntax? tag) => 
            tag == null ? dependency.ToString() : $"{dependency} and tag {tag}";

        public IEnumerable<Dependency> Resolve(SemanticType dependency)
        {
            List<SemanticType> dependencies = new();
            if (!dependency.IsComposedGenericTypeMarker)
            {
                dependencies.Add(dependency);
            }

            if (dependency.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
            {
                var unboundDependency = new SemanticType(namedTypeSymbol.ConstructUnboundGenericType(), dependency);
                dependencies.Add(unboundDependency);
            }

            var fromFactories =
                from pair in _factories
                select (pair.Key, pair.Value.Metadata.Id);

            var fromAuto =
                from pair in _map
                select (pair.Key, pair.Value.Metadata.Id);

            var registeredKeys =
                from item in fromFactories.Concat(fromAuto)
                from dep in dependencies
                where item.Key.Dependency.Equals(dep)
                group item.Key by item.Id into grouped
                select grouped.First();

            foreach (var registeredKey in registeredKeys)
            {
                yield return Resolve(dependency, registeredKey.Tag, ImmutableArray.Create<Location>());
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

            public override string ToString() => Metadata.ToString();
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
