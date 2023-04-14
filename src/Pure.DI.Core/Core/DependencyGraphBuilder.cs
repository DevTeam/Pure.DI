// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using CSharp;

internal class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private readonly IBuilder<MdSetup, IEnumerable<DependencyNode>>[] _dependencyNodeBuilders;
    private readonly IBuilder<ContractsBuildContext, ISet<Injection>> _contractsBuilder;
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;
    private readonly Func<ITypeConstructor> _typeConstructorFactory;
    private readonly Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> _factoryRewriterFactory;
    private readonly IFilter _filter;

    public DependencyGraphBuilder(
        IBuilder<MdSetup, IEnumerable<DependencyNode>>[] dependencyNodeBuilders,
        IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor,
        Func<ITypeConstructor> typeConstructorFactory,
        Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
        IFilter filter)
    {
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _contractsBuilder = contractsBuilder;
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
        _typeConstructorFactory = typeConstructorFactory;
        _factoryRewriterFactory = factoryRewriterFactory;
        _filter = filter;
    }

    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<ProcessingNode> nodes,
        out DependencyGraph? dependencyGraph,
        CancellationToken cancellationToken)
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
        var processed = new HashSet<ProcessingNode>(nodes.Count);
        var notProcessed = new HashSet<ProcessingNode>();
        while (queue.TryDequeue(out var node))
        {
            var targetNode = node.Node;
            foreach (var injection in node.Injections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (map.TryGetValue(injection, out var sourceNode))
                {
                    continue;
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol { IsGenericType: true } geneticType:
                    {
                        // Generic
                        var unboundType = _unboundTypeConstructor.Construct(targetNode.Binding.SemanticModel.Compilation, injection.Type);
                        var unboundInjection = injection with { Type = unboundType };
                        if (map.TryGetValue(unboundInjection, out sourceNode))
                        {
                            var newBinding = CreateGenericBinding(targetNode, injection, sourceNode, ++maxId, cancellationToken);
                            var newNode = CreateNodes(setup, newBinding, cancellationToken)
                                .Single(i => i.Variation == sourceNode.Variation);
                            map[injection] = newNode;
                            queue.Enqueue(CreateNewProcessingNode(injection, newNode));
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

                            if (constructKind.HasValue)
                            {
                                var enumerableBinding = CreateConstructBinding(setup, targetNode, injection, constructType, default, ++maxId, constructKind.Value);
                                return CreateNodes(setup, enumerableBinding, cancellationToken);
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
                        var arrayBinding = CreateConstructBinding(setup, targetNode, injection, arrayType.ElementType, default, ++maxId, MdConstructKind.Array);
                        return CreateNodes(setup, arrayBinding, cancellationToken);
                    }
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: SpecialType.None })
                {
                    var autoBinding = CreateAutoBinding(setup, targetNode, injection, ++maxId);
                    return CreateNodes(setup, autoBinding, cancellationToken).ToArray();
                }
                
                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, targetNode, injection))
                {
                    continue;
                }

                bool TryCreateOnCannotResolve(MdSetup mdSetup, DependencyNode ownerNode, Injection unresolvedInjection)
                {
                    if (mdSetup.Settings.GetState(Setting.OnCannotResolve) == SettingState.On
                        && _filter.IsMeetRegularExpression(
                            mdSetup,
                            (Setting.OnCannotResolveContractTypeNameRegularExpression, unresolvedInjection.Type.ToString()),
                            (Setting.OnCannotResolveTagRegularExpression, unresolvedInjection.Tag.ValueToString()),
                            (Setting.OnCannotResolveLifetimeRegularExpression, ownerNode.Lifetime.ValueToString())))
                    {
                        var onCannotResolveBinding = CreateConstructBinding(mdSetup, ownerNode, unresolvedInjection, unresolvedInjection.Type, unresolvedInjection.Tag, ++maxId, MdConstructKind.OnCannotResolve);
                        var onCannotResolveNodes = CreateNodes(mdSetup, onCannotResolveBinding, cancellationToken);
                        foreach (var onCannotResolveNode in onCannotResolveNodes)
                        {
                            map[unresolvedInjection] = onCannotResolveNode;
                            processed.Add(CreateNewProcessingNode(unresolvedInjection, onCannotResolveNode));
                            return true;
                        }
                    }

                    return false;
                }

                // Not processed
                notProcessed.Add(node);
                isValid = false;
                break;
            }

            if (!isValid)
            {
                break;
            }
            
            processed.Add(node);
        }

        foreach (var key in map.Keys.Where(i => ReferenceEquals(i.Tag, MdTag.ContextTag)))
        {
            map.Remove(key);
        }

        var entries = new List<GraphEntry<DependencyNode, Dependency>>(processed.Count);
        var unresolvedSource = new DependencyNode(0);
        foreach (var node in processed)
        {
            var edges = new List<Dependency>(node.Injections.Length);
            var symbolsWalker = new DependenciesToSymbolsWalker();
            symbolsWalker.VisitDependencyNode(node.Node);
            var symbols = symbolsWalker.ToArray();
            IEnumerable<(Injection injection, ISymbol? symbol)> pairs = symbols.Length == node.Injections.Length
                ? node.Injections.Zip(symbols, (injection, symbol) => (injection, (ISymbol?)symbol))
                : node.Injections.Select(injection => (injection, default(ISymbol)));
            
            foreach (var (injection, symbol) in pairs)
            {
                var dependency = map.TryGetValue(injection, out var sourceNode)
                    ? new Dependency(true, sourceNode, injection, node.Node, symbol)
                    : new Dependency(false, unresolvedSource, injection, node.Node, symbol);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
        }

        if (notProcessed.Any())
        {
            foreach (var node in notProcessed)
            {
                var edges = new List<Dependency>(node.Injections.Length);
                var symbols = new DependenciesToSymbolsWalker();
                symbols.VisitDependencyNode(node.Node);
                foreach (var (injection, symbol) in node.Injections.Zip(symbols, (injection, symbol) => (injection, symbol)))
                {
                    edges.Add(new Dependency(false, unresolvedSource, injection, node.Node, symbol));
                }

                entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
            }
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
        
        if (injection.Tag is { } newTag)
        {
            return new MdTag(0, newTag);
        }

        return default;
    }

    private MdBinding CreateGenericBinding(
        DependencyNode targetNode,
        Injection injection,
        DependencyNode sourceNode,
        int newId,
        CancellationToken cancellationToken)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var compilation = semanticModel.Compilation;
        var typeConstructor = _typeConstructorFactory();
        typeConstructor.Bind(sourceNode.Type, injection.Type);
        var newContracts = sourceNode.Binding.Contracts
            .Select(contract => contract with
            {
                ContractType = typeConstructor.Construct(semanticModel.Compilation, contract.ContractType),
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
                    new RewriterContext<MdFactory>(typeConstructor, injection, sourceNode.Binding.Factory.Value),
                    cancellationToken)
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
            typeConstructor.Bind(injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(compilation, injection.Type);
        }
        
        var newTags = injection.Tag is not null
            ? ImmutableArray.Create(new MdTag(0, injection.Tag))
            : ImmutableArray<MdTag>.Empty;

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ImmutableArray<MdTag>.Empty));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
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
        int newId,
        MdConstructKind constructKind)
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
        return new MdBinding(newId, targetNode.Binding.Source, targetNode.Binding.SemanticModel, newContracts, ImmutableArray<MdTag>.Empty)
        {
            Id = newId,
            SemanticModel = targetNode.Binding.SemanticModel,
            Source = targetNode.Binding.Source,
            Construct = new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContracts.ToImmutableArray())
        };
    }
    
    private ProcessingNode CreateNewProcessingNode(in Injection injection, DependencyNode dependencyNode)
    {
        var contracts = _contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, injection.Tag), CancellationToken.None);
        return new ProcessingNode(dependencyNode, contracts, _marker);
    }

    private IEnumerable<DependencyNode> CreateNodes(
        MdSetup setup,
        MdBinding binding,
        CancellationToken cancellationToken)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        return _dependencyNodeBuilders.SelectMany(builder => builder.Build(newSetup, cancellationToken));
    }
}