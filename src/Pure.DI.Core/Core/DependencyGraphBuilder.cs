// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

internal sealed class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private readonly IReadOnlyCollection<IBuilder<MdSetup, IEnumerable<DependencyNode>>> _dependencyNodeBuilders;
    private readonly IBuilder<ContractsBuildContext, ISet<Injection>> _contractsBuilder;
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;
    private readonly Func<ITypeConstructor> _typeConstructorFactory;
    private readonly Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> _factoryRewriterFactory;
    private readonly IFilter _filter;
    private readonly CancellationToken _cancellationToken;

    public DependencyGraphBuilder(
        IReadOnlyCollection<IBuilder<MdSetup, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
        IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor,
        Func<ITypeConstructor> typeConstructorFactory,
        Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
        IFilter filter,
        CancellationToken cancellationToken)
    {
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _contractsBuilder = contractsBuilder;
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
        _typeConstructorFactory = typeConstructorFactory;
        _factoryRewriterFactory = factoryRewriterFactory;
        _filter = filter;
        _cancellationToken = cancellationToken;
    }

    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<ProcessingNode> nodes,
        out DependencyGraph? dependencyGraph)
    {
        dependencyGraph = default;
        var maxId = 0;
        var map = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var queue = new Queue<ProcessingNode>();
        foreach (var processingNode in nodes)
        {
            var node = processingNode.Node;
            if (node.Binding.Id > maxId)
            {
                maxId = node.Binding.Id;
            }

            if (node.Root is null)
            {
                foreach (var contract in processingNode.Contracts)
                {
                    map[contract] = node;
                }
            }

            if (!processingNode.IsMarkerBased)
            {
                queue.Enqueue(processingNode);
            }
        }

        var isValid = true;
        var processed = new HashSet<ProcessingNode>();
        var notProcessed = new HashSet<ProcessingNode>();
        var edgesMap = new Dictionary<ProcessingNode, List<Dependency>>();
        while (queue.TryDequeue(out var node))
        {
            var targetNode = node.Node;
            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue) in node.Injections)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (map.TryGetValue(injection, out var sourceNode))
                {
                    continue;
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol { IsGenericType: true } geneticType:
                    {
                        // Generic
                        var isGenericOk = false;
                        foreach (var item in map)
                        {
                            if (!Injection.EqualTags(injection.Tag, item.Key.Tag))
                            {
                                continue;
                            }

                            if (item.Key.Type is not INamedTypeSymbol { IsGenericType: true })
                            {
                                continue;
                            }
                            
                            var typeConstructor = _typeConstructorFactory();
                            if (!typeConstructor.TryBind(item.Key.Type, injection.Type))
                            {
                                continue;
                            }

                            sourceNode = item.Value;
                            var genericBinding = CreateGenericBinding(targetNode, injection, sourceNode, typeConstructor, ++maxId);
                            var genericNode = CreateNodes(setup, genericBinding).Single(i => i.Variation == sourceNode.Variation);
                            map[injection] = genericNode;
                            queue.Enqueue(CreateNewProcessingNode(injection, genericNode));
                            isGenericOk = true;
                            break;
                        }

                        if (isGenericOk)
                        {
                            continue;
                        }

                        // Construct
                        if (geneticType.TypeArguments is [{ } constructType])
                        {
                            var constructKind = geneticType.ConstructUnboundGenericType().ToString() switch
                            {
                                "System.Span<>" => MdConstructKind.Span,
                                "System.ReadOnlySpan<>" => MdConstructKind.Span,
                                "System.Collections.Generic.IEnumerable<>" => MdConstructKind.Enumerable,
                                _ => default(MdConstructKind?)
                            };

                            var lifetime = constructKind == MdConstructKind.Enumerable ? Lifetime.PerResolve : Lifetime.Transient;
                            if (constructKind.HasValue)
                            {
                                var enumerableBinding = CreateConstructBinding(setup, targetNode, injection, constructType, default, lifetime, ++maxId, constructKind.Value, false, default);
                                return CreateNodes(setup, enumerableBinding);
                            }
                        }
                        
                        // OnCannotResolve
                        if (TryCreateOnCannotResolve(setup, targetNode, injection))
                        {
                            continue;
                        }

                        break;
                    }

                    // Array construct
                    case IArrayTypeSymbol arrayType:
                    {
                        var arrayBinding = CreateConstructBinding(setup, targetNode, injection, arrayType.ElementType, default, Lifetime.Transient, ++maxId, MdConstructKind.Array, false, default);
                        return CreateNodes(setup, arrayBinding);
                    }
                }
                
                // ExplicitDefaultValue
                if (hasExplicitDefaultValue)
                {
                    var explicitDefaultBinding = CreateConstructBinding(setup, targetNode, injection, injection.Type, default, Lifetime.Transient, ++maxId, MdConstructKind.ExplicitDefaultValue, hasExplicitDefaultValue, explicitDefaultValue);
                    var newSourceNodes = CreateNodes(setup, explicitDefaultBinding);
                    foreach (var newSourceNode in newSourceNodes)
                    {
                        if (!edgesMap.TryGetValue(node, out var edges))
                        {
                            edges = new List<Dependency>();
                            edgesMap.Add(node, edges);
                        }

                        edges.Add(new Dependency(true, newSourceNode, injection, targetNode));
                    }
                    
                    continue;
                }

                if (injection.Type.ToString() == setup.Name.FullName)
                {
                    // Composition
                    var compositionBinding = CreateConstructBinding(setup, targetNode, injection, injection.Type, default, Lifetime.Transient, ++maxId, MdConstructKind.Composition, false, default);
                    return CreateNodes(setup, compositionBinding);
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: SpecialType.None })
                {
                    var autoBinding = CreateAutoBinding(setup, targetNode, injection, ++maxId);
                    return CreateNodes(setup, autoBinding).ToArray();
                }
                
                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, targetNode, injection))
                {
                    continue;
                }

                // Not processed
                notProcessed.Add(node);
                isValid = false;
                break;

                bool TryCreateOnCannotResolve(MdSetup mdSetup, DependencyNode ownerNode, Injection unresolvedInjection)
                {
                    if (mdSetup.Hints.GetHint(Hint.OnCannotResolve) == SettingState.On
                        && _filter.IsMeetRegularExpression(
                            mdSetup,
                            (Hint.OnCannotResolveContractTypeNameRegularExpression, unresolvedInjection.Type.ToString()),
                            (Hint.OnCannotResolveTagRegularExpression, unresolvedInjection.Tag.ValueToString()),
                            (Hint.OnCannotResolveLifetimeRegularExpression, ownerNode.Lifetime.ValueToString())))
                    {
                        var onCannotResolveBinding = CreateConstructBinding(mdSetup, ownerNode, unresolvedInjection, unresolvedInjection.Type, unresolvedInjection.Tag, ownerNode.Lifetime, ++maxId, MdConstructKind.OnCannotResolve, false, default);
                        var onCannotResolveNodes = CreateNodes(mdSetup, onCannotResolveBinding);
                        foreach (var onCannotResolveNode in onCannotResolveNodes)
                        {
                            map[unresolvedInjection] = onCannotResolveNode;
                            processed.Add(CreateNewProcessingNode(unresolvedInjection, onCannotResolveNode));
                            return true;
                        }
                    }

                    return false;
                }
            }

            if (!isValid)
            {
                break;
            }
            
            processed.Add(node);
        }

        foreach (var key in map.Keys.Where(i => ReferenceEquals(i.Tag, MdTag.ContextTag)).ToArray())
        {
            map.Remove(key);
        }

        var entries = new List<GraphEntry<DependencyNode, Dependency>>(processed.Count);
        foreach (var node in processed.Concat(notProcessed))
        {
            if (!edgesMap.TryGetValue(node, out var edges))
            {
                edges = new List<Dependency>();
                edgesMap.Add(node, edges);
            }

            foreach (var injectionInfo in node.Injections)
            {
                var injection = injectionInfo.Injection;
                var dependency = map.TryGetValue(injection, out var sourceNode)
                    ? new Dependency(true, sourceNode, injection, node.Node)
                    : new Dependency(false, new DependencyNode(0), injection, node.Node);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
        }
        
        dependencyGraph = new DependencyGraph(
            isValid,
            setup,
            new Graph<DependencyNode, Dependency>(entries.ToImmutableArray()),
            map,
            ImmutableDictionary<Injection, Root>.Empty);
        
        return ImmutableArray<DependencyNode>.Empty;
    }
    
    private static MdTag? CreateTag(in Injection injection, in MdTag? tag)
    {
        if (!tag.HasValue || !ReferenceEquals(tag.Value.Value, MdTag.ContextTag))
        {
            return tag;
        }

        return new MdTag(0, injection.Tag);
    }

    private MdBinding CreateGenericBinding(
        DependencyNode targetNode,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int newId)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var compilation = semanticModel.Compilation;
        var newContracts = sourceNode.Binding.Contracts
            .Select(contract => contract with
            {
                ContractType = injection.Type,
                Tags = contract.Tags.Select( tag => CreateTag(injection, tag)).Where(tag => tag.HasValue).Select(tag => tag!.Value).ToImmutableArray()
            })
            .ToImmutableArray();

        return sourceNode.Binding with
        {
            Id = newId,
            Contracts = newContracts,
            Implementation = sourceNode.Binding.Implementation.HasValue
                ? sourceNode.Binding.Implementation.Value with
                {
                    Type = typeConstructor.Construct(compilation, sourceNode.Binding.Implementation.Value.Type)
                }
                : default(MdImplementation?),
            Factory = sourceNode.Binding.Factory.HasValue
                ? _factoryRewriterFactory().Build(
                    new RewriterContext<MdFactory>(typeConstructor, injection, sourceNode.Binding.Factory.Value))
                : default(MdFactory?),
            Arg = sourceNode.Binding.Arg.HasValue
                ? sourceNode.Binding.Arg.Value with
                {
                    Type = typeConstructor.Construct(compilation, sourceNode.Binding.Arg.Value.Type)
                }
                : default(MdArg?)
        };
    }

    private MdBinding CreateAutoBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        int newId)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var compilation = semanticModel.Compilation;
        var sourceType = injection.Type;
        var typeConstructor = _typeConstructorFactory();
        if (_marker.IsMarkerBased(injection.Type))
        {
            typeConstructor.TryBind(injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(compilation, injection.Type);
        }
        
        var newTags = injection.Tag is not null
            ? ImmutableArray.Create(new MdTag(0, injection.Tag))
            : ImmutableArray<MdTag>.Empty;

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ImmutableArray<MdTag>.Empty));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            semanticModel,
            newContracts,
            newTags,
            new MdLifetime(semanticModel, setup.Source, Lifetime.Transient),
            new MdImplementation(semanticModel, setup.Source, sourceType));
        return newBinding;
    }

    private static MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        object? tag,
        Lifetime lifetime,
        int newId,
        MdConstructKind constructKind,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue)
    {
        elementType = elementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var dependencyContracts = new List<MdContract>();
        foreach (var nestedBinding in setup.Bindings.Where(i => i != targetNode.Binding))
        {
            var matchedContracts = nestedBinding.Contracts.Where(i => SymbolEqualityComparer.Default.Equals(i.ContractType, elementType)).ToImmutableArray();
            if (!matchedContracts.Any())
            {
                continue;
            }

            var tags = matchedContracts.First().Tags
                .Concat(nestedBinding.Tags)
                .Select((i, position) => i with { Position = position })
                .ToImmutableArray();
            dependencyContracts.Add(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, tags));
        }

        var newTags = tag is not null
            ? ImmutableArray.Create(new MdTag(0, tag))
            : ImmutableArray<MdTag>.Empty;
        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, newTags));
        return new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            newContracts,
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, targetNode.Binding.Source, lifetime),
            default,
            default,
            default,
            new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContracts.ToImmutableArray(),
                hasExplicitDefaultValue,
                explicitDefaultValue));
    }
    
    private ProcessingNode CreateNewProcessingNode(in Injection injection, DependencyNode dependencyNode)
    {
        var contracts = _contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, injection.Tag));
        return new ProcessingNode(dependencyNode, contracts, _marker);
    }

    private IEnumerable<DependencyNode> CreateNodes(MdSetup setup, MdBinding binding)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        return _dependencyNodeBuilders.SelectMany(builder => builder.Build(newSetup));
    }
}