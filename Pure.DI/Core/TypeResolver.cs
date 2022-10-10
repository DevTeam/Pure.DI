// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToReturnStatement
namespace Pure.DI.Core;

using NS35EBD81B;

[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
// ReSharper disable once ClassNeverInstantiated.Global
internal class TypeResolver : ITypeResolver
{
    private readonly IDiagnostic _diagnostic;
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
    private readonly List<DefaultFactory> _defaultFactories = new();
    
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
        _diagnostic = diagnostic;
        _buildContext = buildContext;
        _typesMapFactory = typesMapFactory;
        _constructorBuilder = constructorBuilder;
        _factoryBuilder = factoryBuilder;
        _arrayBuilder = arrayBuilder;
        _enumerableBuilder = enumerableBuilder;
        foreach (var binding in metadata.Bindings.Where(i => i.BindingType is BindingType.Default or BindingType.Arg))
        {
            AddBinding(binding, diagnostic);
        }

        _defaultFactories.Sort();
    }

    private void AddBinding(IBindingMetadata binding, IDiagnostic? diagnostic = default)
    {
        if (binding.Implementation == null)
        {
            return;
        }

        _implementations.Add(binding.Implementation);
        var dependencies = new HashSet<SemanticType>(binding.Dependencies);
        foreach (var type in dependencies)
        {
            var isGenericTypeMarker = new Lazy<bool>(() => type.IsGenericTypeMarker);
            if (binding.Factory == default && (binding.Implementation.IsGenericTypeMarker || isGenericTypeMarker.Value))
            {
                _diagnostic.Error(
                    Diagnostics.Error.InvalidSetup,
                    $"The type {type} is a generic type marker. It cannot be used in non-factory bindings directly outside of generic types.",
                    new []{ binding.Location }.Where(i => i != default).Select(i => i!).ToArray());
                continue;
            }
            
            foreach (var tag in binding.GetTags(type).DefaultIfEmpty<ExpressionSyntax?>(null))
            {
                var unboundType = type.ConstructUnbound();
                if (
                    !GetSpecialTypes(unboundType).Contains(unboundType)
                    && type.SemanticModel.Compilation.GetTypeByMetadataName(unboundType.Name) == null
                    && type.Type is INamedTypeSymbol)
                {
                    continue;
                }

                var key = new Key(unboundType, tag, binding.AnyTag);
                if (diagnostic != default && _map.TryGetValue(key, out var prev))
                {
                    diagnostic.Information(Diagnostics.Information.BindingAlreadyExists, $"{prev.Metadata} exists and will be overridden by a new one {binding}.");
                }

                _map[key] = new Binding<SemanticType>(binding, binding.Implementation);

                if (binding.Factory != null)
                {
                    var factoryBinding = new Binding<SimpleLambdaExpressionSyntax>(binding, binding.Factory);
                    if (!isGenericTypeMarker.Value)
                    {
                        _factories[key] = factoryBinding;
                    }
                    else
                    {
                        if (!TryFindDefaultFactory(type.Type, true, out _))
                        {
                            _defaultFactories.Add(new DefaultFactory(type.Type, factoryBinding));
                        }
                    }
                }

                if (binding.Lifetime is Lifetime.Scoped or Lifetime.ContainerSingleton)
                {
                    var serviceProviderInstance = new SemanticType(type.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance<>).FullName.ReplaceNamespace())!, type.SemanticModel).Construct(type);
                    _buildContext.AddBinding(new BindingMetadata(binding, serviceProviderInstance, binding.Id));
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
                        new Key(typeSelector(dependency), tag, anyTag), new Key(typeSelector(unboundDependency), tag, anyTag)
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
                            
                            if (key.Dependency.Equals(dependency))
                            {
                                return new Dependency(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorBuilder(), typesMap);
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

                                AddBinding(binding);
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
                    var realDependency = typeSelector(dependency);
                    var key = new Key(realDependency, tag);
                    if (_map.TryGetValue(key, out var implementationEntry))
                    {
                        var typesMap = _typesMapFactory();
                        typesMap.Setup(implementationEntry.Details, realDependency);
                        var resolvedDependency = _factories.TryGetValue(key, out var factory)
                            ? new Dependency(factory.Metadata, realDependency, tag, _factoryBuilder(), typesMap)
                            : new Dependency(implementationEntry.Metadata, implementationEntry.Details, tag, _constructorBuilder(), typesMap);

                        return resolvedDependency;
                    }
                }

                if (
                    !dependency.Type.IsAbstract
                    && !_implementations.Contains(dependency)
                    && !dependency.Name.StartsWith("System.Func`")
                    && !GetSpecialTypes(dependency.SemanticModel).Contains(dependency)
                    && dependency.IsValidTypeToResolve)
                {
                    var typesMap = _typesMapFactory();
                    var newBinding = new BindingMetadata
                    {
                        Implementation = dependency,
                        Lifetime = Lifetime.Transient,
                        BindingType = BindingType.Probe
                    };

                    newBinding.AddDependency(dependency);
                    AddBinding(newBinding);
                    return new Dependency(newBinding, dependency, tag, _constructorBuilder(), typesMap);
                }

                if (TryFindDefaultFactory(dependency.Type, false, out var defaultFactory))
                {
                    return CreateDefaultFactoryDependency(dependency, tag, defaultFactory);
                }
                
                break;
            }

            case IArrayTypeSymbol arrayTypeSymbol:
                var resolved = Resolve(
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

        return new Dependency(
            new BindingMetadata(dependency) { BindingType = BindingType.Probe },
            dependency,
            null,
            _constructorBuilder(),
            _typesMapFactory(),
            false);
    }

    public IEnumerable<Dependency> Resolve(SemanticType dependency)
    {
        List<SemanticType> dependencies = new();
        if (!dependency.IsComposedGenericTypeMarker)
        {
            dependencies.Add(dependency);
        }

        if (dependency.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
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
            group item.Key by item.Id
            into grouped
            select grouped.First();

        foreach (var registeredKey in registeredKeys)
        {
            yield return Resolve(dependency, registeredKey.Tag);
        }
    }

    private bool TryFindDefaultFactory(ITypeSymbol type, bool add, out Binding<SimpleLambdaExpressionSyntax> factory)
    {
        foreach (var defaultFactory in _defaultFactories)
        {
            if (defaultFactory.TryGetFactory(type, add, out factory))
            {
                return true;
            }
        }

        factory = default;
        return false;
    }
    
    private Dependency CreateDefaultFactoryDependency(SemanticType dependency, ExpressionSyntax? tag, Binding<SimpleLambdaExpressionSyntax> defaultBinding)
    {
        var typesMap = _typesMapFactory();
        foreach (var defaultDependency in defaultBinding.Metadata.Dependencies)
        {
            typesMap.Setup(defaultDependency, dependency);
        }

        return new Dependency(defaultBinding.Metadata, dependency, tag, _factoryBuilder(), typesMap);
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
    
    private class DefaultFactory: IComparable<DefaultFactory>
    {
        private readonly ITypeSymbol _type;
        private readonly Binding<SimpleLambdaExpressionSyntax> _factory;

        public DefaultFactory(ITypeSymbol type, Binding<SimpleLambdaExpressionSyntax> factory)
        {
            _type = type;
            _factory = factory;
        }

        public bool TryGetFactory(ITypeSymbol type, bool add, out Binding<SimpleLambdaExpressionSyntax> factory)
        {
            if (add)
            {
                if (
                    _type.IsValueType != type.IsValueType 
                    || _type.IsReferenceType != type.IsReferenceType
                    || type.IsAbstract)
                {
                    factory = default;
                    return false;
                }
                
                if (_type.AllInterfaces.Except(type.AllInterfaces).Any())
                {
                    factory = default;
                    return false; 
                }
            }
            else
            {
                if (
                    _type.IsValueType != type.IsValueType 
                    && _type.IsReferenceType != type.IsReferenceType
                    && !_type.IsAbstract)
                {
                    factory = default;
                    return false;
                }
                
                if (_type.AllInterfaces.Any() && !_type.AllInterfaces.Contains(type))
                {
                    factory = default;
                    return false; 
                }
            }

            factory = _factory;
            return true;
        }

        public int CompareTo(DefaultFactory other)
        {
            var otherWeight = other._type.AllInterfaces.Length - (other._type.IsAbstract ? 1000 : 0);
            var weight = _type.AllInterfaces.Length - (_type.IsAbstract ? 1000 : 0);
            return otherWeight - weight;
        }
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