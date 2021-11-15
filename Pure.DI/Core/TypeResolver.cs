// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToReturnStatement
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
        private readonly IBuildContext _buildContext;
        private readonly Func<ITypesMap> _typesMapFactory;
        private readonly Func<IObjectBuilder> _constructorBuilder;
        private readonly Func<IObjectBuilder> _factoryBuilder;
        private readonly Func<IObjectBuilder> _arrayBuilder;
        private readonly Func<IObjectBuilder> _enumerableBuilder;
        private readonly Dictionary<Key, Binding<SemanticType>> _map = new();
        private readonly Dictionary<Key, Binding<SimpleLambdaExpressionSyntax>> _factories = new();
        private readonly HashSet<SemanticType> _implementations = new(SemanticTypeEqualityComparer.Default);
        private readonly HashSet<SemanticType> _specialTypes = new(SemanticTypeEqualityComparer.Default);

        public TypeResolver(
            ResolverMetadata metadata,
            IDiagnostic diagnostic,
            IBuildContext buildContext,
            Func<ITypesMap> typesMapFactory,
            [Tag(Tags.AutowiringBuilder)] Func<IObjectBuilder> constructorBuilder,
            [Tag(Tags.FactoryBuilder)] Func<IObjectBuilder> factoryBuilder,
            [Tag(Tags.ArrayBuilder)] Func<IObjectBuilder> arrayBuilder,
            [Tag(Tags.EnumerableBuilder)] Func<IObjectBuilder> enumerableBuilder)
        {
            _buildContext = buildContext;
            _typesMapFactory = typesMapFactory;
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

                _implementations.Add(binding.Implementation);
                var dependencies = new HashSet<SemanticType>(binding.Dependencies);
                foreach (var type in dependencies)
                {
                    foreach (var tag in binding.GetTags(type).DefaultIfEmpty<ExpressionSyntax?>(null))
                    {
                        SemanticType unboundType = type.ConstructUnbound();
                        if (
                            !GetSpecialTypes(unboundType).Contains(unboundType)
                            && type.SemanticModel.Compilation.GetTypeByMetadataName(unboundType.Name) == null
                            && type.Type is INamedTypeSymbol)
                        {
                            continue;
                        }

                        var key = new Key(unboundType, tag, binding.AnyTag);
                        if (_map.TryGetValue(key, out var prev))
                        {
                            diagnostic.Information(Diagnostics.Information.BindingAlreadyExists, $"{prev.Metadata} exists and will be overridden by a new one {binding}.");
                        }

                        _map[key] = new Binding<SemanticType>(binding, binding.Implementation);

                        if (binding.Factory != null)
                        {
                            _factories[key] = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                        }

                        if (binding.Lifetime is Lifetime.Scoped or Lifetime.ContainerSingleton)
                        {
                            var serviceProviderInstance = new SemanticType(type.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance<>).FullName)!, type.SemanticModel).Construct(type);
                            _buildContext.AddBinding(new BindingMetadata(binding, serviceProviderInstance, binding.Id));
                        }
                    }
                }
            }
        }
        
        public Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, bool anyTag = false) =>
            Resolve(dependency, tag, anyTag, i => i);

        private Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, bool anyTag, Func<SemanticType, SemanticType> typeSelector)
        {
            switch (dependency.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    if (namedType.IsGenericType)
                    {
                        var unboundDependency = new SemanticType(namedType.ConstructUnboundGenericType(), dependency);
                        var keys = new[]
                        {
                            new Key(typeSelector(dependency), tag, anyTag),
                            new Key(typeSelector(unboundDependency), tag, anyTag)
                        };

                        foreach (var key in keys)
                        {
                            if (_map.TryGetValue(key, out var implementationEntry))
                            {
                                var typesMap = _typesMapFactory();
                                var dep = typeSelector(dependency);
                                var hasTypesMap = typesMap.Setup(implementationEntry.Details, dep);
                                var constructedImplementation = typesMap.ConstructType(implementationEntry.Details);
                                var constructedDependency = typesMap.ConstructType(dep);
                                if (_factories.TryGetValue(key, out var factory))
                                {
                                   return new Dependency(factory.Metadata, dep, tag, _factoryBuilder(), typesMap);
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
                                        binding.AddTags(tag);
                                    }

                                    binding.AddDependency(constructedDependency);
                                    binding.AddDependency(constructedImplementation);

                                    _buildContext.AddBinding(binding);
                                    return new Dependency(implementationEntry.Metadata, constructedImplementation, tag, _constructorBuilder(), typesMap);
                                }
                            }
                        }

                        if (unboundDependency.Equals(typeof(IEnumerable<>)))
                        {
                            return new Dependency(new BindingMetadata(dependency), typeSelector(dependency), null, _enumerableBuilder(), _typesMapFactory());
                        }
                    }
                    else
                    {
                        SemanticType dependency1 = typeSelector(dependency);
                        Dependency? resolvedDependency;
                        bool ret;
                        var key = new Key(dependency1, tag);
                        if (_map.TryGetValue(key, out var implementationEntry))
                        {
                            var typesMap = _typesMapFactory();
                            typesMap.Setup(implementationEntry.Details, dependency1);
                            resolvedDependency = _factories.TryGetValue(key, out var factory)
                                ? new Dependency(factory.Metadata, dependency1, tag, _factoryBuilder(), typesMap)
                                : new Dependency(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorBuilder(), typesMap);

                            ret = true;
                        }
                        else
                        {
                            resolvedDependency = default;
                            ret = false;
                        }

                        if (ret)
                        {
                            return resolvedDependency!.Value;
                        }
                    }

                    if (
                        !dependency.Type.IsAbstract
                        && !_implementations.Contains(dependency)
                        && !GetSpecialTypes(dependency.SemanticModel).Contains(dependency)
                        && dependency.IsValidTypeToResolve)
                    {
                        var typesMap = _typesMapFactory();
                        var newBinding = new BindingMetadata
                        {
                            Implementation = dependency,
                            Lifetime = Lifetime.Transient,
                            FromProbe = true
                        };

                        newBinding.AddDependency(dependency);
                        return new Dependency(newBinding, dependency, tag, _constructorBuilder(), typesMap);
                    }

                    break;
                }

                case IArrayTypeSymbol arrayTypeSymbol:
                    var resolved =  Resolve(
                        new SemanticType(arrayTypeSymbol.ElementType, dependency),
                        tag,
                        anyTag,
                        type => new SemanticType(type.SemanticModel.Compilation.CreateArrayTypeSymbol(type.Type), type));

                    if (resolved.IsResolved)
                    {
                        return resolved;
                    }

                    return new Dependency(new BindingMetadata(dependency), dependency, null, _arrayBuilder(), _typesMapFactory());
            }

            return new Dependency(new BindingMetadata(dependency) { FromProbe = true }, dependency, null, _constructorBuilder(), _typesMapFactory(), false);
        }

        public IEnumerable<Dependency> Resolve(SemanticType dependency)
        {
            List<SemanticType> dependencies = new();
            if (!dependency.IsComposedGenericTypeMarker)
            {
                dependencies.Add(dependency);
            }

            if (dependency.Type is INamedTypeSymbol {IsGenericType: true} namedTypeSymbol)
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
                yield return Resolve(dependency, registeredKey.Tag);
            }
        }

        private readonly struct Binding<T>
        {
            public readonly IBindingMetadata Metadata;
            public readonly T Details;

            public Binding(IBindingMetadata metadata, T details)
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
