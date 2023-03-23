// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

internal class DependencyGraphBuilder : IBuilder<MdSetup, DependencyGraph>
{
    private readonly ILogger<DependencyGraphBuilder> _logger;
    private readonly IBuilder<MdSetup, IEnumerable<DependencyNode>>[] _dependencyNodeBuilders;
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private readonly Func<ITypeConstructor> _typeConstructorFactory;
    private readonly Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> _factoryRewriterFactory;

    public DependencyGraphBuilder(
        ILogger<DependencyGraphBuilder> logger,
        IBuilder<MdSetup, IEnumerable<DependencyNode>>[] dependencyNodeBuilders,
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor,
        IBuilder<MdBinding, ISet<Injection>> injectionsBuilder,
        Func<ITypeConstructor> typeConstructorFactory,
        Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory)
    {
        _logger = logger;
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
        _injectionsBuilder = injectionsBuilder;
        _typeConstructorFactory = typeConstructorFactory;
        _factoryRewriterFactory = factoryRewriterFactory;
    }

    public DependencyGraph Build(MdSetup setup, CancellationToken cancellationToken)
    {
        var map = new Dictionary<Injection, DependencyNode>();
        var contracts = new Dictionary<MdBinding, ImmutableArray<Injection>>();
        var resolved = new List<ResolvingNode>();
        var unresolved = new Dictionary<MdBinding, ResolvingNode>();
        var nextGroups = SplitNodes(_dependencyNodeBuilders.SelectMany(builder => builder.Build(setup, cancellationToken))).ToList();
        var id = 0;
        do
        {
            var processingGroups = nextGroups.ToImmutableArray();
            nextGroups.Clear();
            foreach (var group in processingGroups)
            {
                var processing = new List<DependencyNode>();
                foreach (var node in group)
                {
                    if (contracts.ContainsKey(node.Binding))
                    {
                        continue;
                    }

                    var binding = node.Binding;
                    if (binding.Id > id)
                    {
                        id = binding.Id;
                    }

                    var nodeInjections = _injectionsBuilder.Build(binding, cancellationToken);
                    var newInjections = nodeInjections.Except(map.Keys).ToImmutableArray();
                    if (node.Root is not { })
                    {
                        if (!newInjections.Any())
                        {
                            _logger.CompileWarning("Overridden binding.", binding.Source.GetLocation(), LogId.WarningOverriddenBinding);
                        }

                        if (newInjections.Length < nodeInjections.Count)
                        {
                            var overriddenInjections = nodeInjections.Except(newInjections);
                            _logger.CompileWarning($"Partially overridden binding: {string.Join(", ", overriddenInjections)}.", binding.Source.GetLocation(), LogId.WarningOverriddenBinding);
                        }
                    }

                    contracts.Add(binding, newInjections);
                    foreach (var missingInjection in newInjections)
                    {
                        map.Add(missingInjection, node);
                    }

                    processing.Add(node);
                }

                var unresolvedBindings = new HashSet<MdBinding>();
                while (processing.Count > 0)
                {
                    var targetNodes = processing.ToImmutableArray();
                    processing.Clear();
                    foreach (var targetNode in targetNodes)
                    {
                        if (_marker.IsMarkerBased(targetNode.Type))
                        {
                            continue;
                        }

                        var semanticModel = targetNode.Binding.SemanticModel;
                        var compilation = semanticModel.Compilation;
                        var isResolved = true;
                        var injectionsWalker = new DependenciesToInjectionsWalker();
                        injectionsWalker.VisitDependencyNode(targetNode);
                        var nodeInjections = injectionsWalker.ToImmutableArray();
                        foreach (var injection in nodeInjections)
                        {
                            // Simple
                            if (map.TryGetValue(injection, out var sourceNode))
                            {
                                continue;
                            }

                            if (injection.Type is INamedTypeSymbol { IsGenericType: true } geneticType)
                            {
                                // Generic
                                var unboundType = _unboundTypeConstructor.Construct(compilation, injection.Type);
                                var unboundInjection = injection with { Type = unboundType };
                                if (map.TryGetValue(unboundInjection, out sourceNode))
                                {
                                    var typeConstructor = _typeConstructorFactory();
                                    typeConstructor.Bind(sourceNode.Type, injection.Type);
                                    var newContracts = sourceNode.Binding.Contracts.Select(contract => contract with { ContractType = typeConstructor.Construct(semanticModel.Compilation, contract.ContractType) }).ToImmutableArray();
                                    id++;
                                    var newBinding = sourceNode.Binding with
                                    {
                                        Id = id,
                                        Contracts = newContracts,
                                        Implementation = sourceNode.Binding.Implementation.HasValue
                                            ? sourceNode.Binding.Implementation.Value with
                                            {
                                                Type = typeConstructor.Construct(compilation, sourceNode.Binding.Implementation.Value.Type)
                                            }
                                            : default(MdImplementation?),
                                        Factory = sourceNode.Binding.Factory.HasValue
                                            ? _factoryRewriterFactory().Build(
                                                new RewriterContext<MdFactory>(typeConstructor, sourceNode.Binding.Factory.Value),
                                                cancellationToken)
                                            : default(MdFactory?),
                                        Arg = sourceNode.Binding.Arg.HasValue
                                            ? sourceNode.Binding.Arg.Value with
                                            {
                                                Type = typeConstructor.Construct(compilation, sourceNode.Binding.Arg.Value.Type)
                                            }
                                            : default(MdArg?)
                                    };


                                    nextGroups.AddRange(AddBinding(setup, newBinding, cancellationToken));
                                    continue;
                                }

                                // Enumerable
                                if (geneticType.TypeArguments is [{ } enumerableType]
                                    && geneticType.ConstructUnboundGenericType().ToString() == "System.Collections.Generic.IEnumerable<>")
                                {
                                    id++;
                                    var enumerableBinding = CreateConstructBinding(setup, targetNode, injection, enumerableType, id, MdConstructKind.Enumerable);
                                    nextGroups.AddRange(AddBinding(setup, enumerableBinding, cancellationToken));
                                    continue;
                                }
                            }
                            
                            // Array
                            if (injection.Type is IArrayTypeSymbol arrayType)
                            {
                                id++;
                                var arrayBinding = CreateConstructBinding(setup, targetNode, injection, arrayType.ElementType, id, MdConstructKind.Array);
                                nextGroups.AddRange(AddBinding(setup, arrayBinding, cancellationToken));
                                continue;
                            }

                            // Auto-binding
                            if (injection.Type is { IsAbstract: false, SpecialType: SpecialType.None })
                            {
                                var sourceType = injection.Type;
                                var typeConstructor = _typeConstructorFactory();
                                if (_marker.IsMarkerBased(injection.Type))
                                {
                                    typeConstructor.Bind(injection.Type, injection.Type);
                                    sourceType = typeConstructor.Construct(compilation, injection.Type);
                                }

                                var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ImmutableArray<MdTag>.Empty));
                                id++;
                                var newBinding = new MdBinding(
                                    id,
                                    targetNode.Binding.Source,
                                    semanticModel,
                                    newContracts,
                                    ImmutableArray<MdTag>.Empty, 
                                    new MdLifetime(semanticModel, setup.Source, Lifetime.Transient),
                                    new MdImplementation(semanticModel, setup.Source, sourceType));

                                nextGroups.AddRange(AddBinding(setup, newBinding, cancellationToken));
                                continue;
                            }

                            isResolved = false;
                            break;
                        }

                        if (isResolved)
                        {
                            unresolved.Remove(targetNode.Binding);
                            resolved.Add(new ResolvingNode(targetNode, nodeInjections));
                        }
                        else
                        {
                            unresolvedBindings.Add(targetNode.Binding);
                            unresolved.Add(targetNode.Binding, new ResolvingNode(targetNode, nodeInjections));
                        }
                    }
                }

                foreach (var unresolvedBinding in unresolvedBindings)
                {
                    if (contracts.TryGetValue(unresolvedBinding, out var unresolvedContracts))
                    {
                        foreach (var unresolvedContract in unresolvedContracts)
                        {
                            map.Remove(unresolvedContract);
                        }
                    }

                    contracts.Remove(unresolvedBinding);
                }
            }
        } while (nextGroups.Any());

        var entriesBuilder = ImmutableArray.CreateBuilder<GraphEntry<DependencyNode, Dependency>>();
        var unresolvedSource = new DependencyNode();
        foreach (var (targetNode, targetInjections) in resolved)
        {
            var edges = ImmutableArray.CreateBuilder<Dependency>();
            foreach (var targetInjection in targetInjections)
            {
                edges.Add(
                    map.TryGetValue(targetInjection, out var sourceNode)
                        ? new Dependency(true, sourceNode, targetInjection, targetNode)
                        : new Dependency(false, unresolvedSource, targetInjection, targetNode));
            }
            
            entriesBuilder.Add(new GraphEntry<DependencyNode, Dependency>(targetNode, edges.ToImmutableArray()));
        }
        
        // ReSharper disable once InvertIf
        if (unresolved.Any())
        {
            var edges = ImmutableArray.CreateBuilder<Dependency>();
            foreach (var (unresolvedTargetNode, unresolvedInjections) in unresolved.Values)
            {
                foreach (var unresolvedInjection in unresolvedInjections)
                {
                    edges.Add(new Dependency(false, unresolvedSource, unresolvedInjection, unresolvedTargetNode));
                }
                
                entriesBuilder.Add(new GraphEntry<DependencyNode, Dependency>(unresolvedTargetNode, edges.ToImmutableArray()));
            }
        }

        return new DependencyGraph(
            setup,
            new Graph<DependencyNode, Dependency>(entriesBuilder.ToImmutable()),
            map,
            ImmutableDictionary<Injection, Root>.Empty);
    }

    private static MdBinding CreateConstructBinding(MdSetup setup,
        DependencyNode node,
        Injection injection,
        ITypeSymbol elementType,
        int newId,
        MdConstructKind constructKind)
    {
        var dependencyContractsBuilder = ImmutableArray.CreateBuilder<MdContract>();
        foreach (var nestedBinding in setup.Bindings.Where(i => i != node.Binding))
        {
            var matchedContracts = nestedBinding.Contracts.Where(i => SymbolEqualityComparer.Default.Equals(i.ContractType, elementType)).ToImmutableArray();
            if (!matchedContracts.Any())
            {
                continue;
            }

            var tag = matchedContracts.First().Tags.Concat(nestedBinding.Tags).Select(i => i.Value).FirstOrDefault();
            var tags = tag is { }
                ? ImmutableArray.Create(new MdTag(node.Binding.SemanticModel, node.Binding.Source, 0, tag))
                : ImmutableArray<MdTag>.Empty;
            dependencyContractsBuilder.Add(new MdContract(node.Binding.SemanticModel, node.Binding.Source, elementType, tags));
        }

        var newContracts = ImmutableArray.Create(new MdContract(node.Binding.SemanticModel, node.Binding.Source, injection.Type, ImmutableArray<MdTag>.Empty));
        return new MdBinding(newId, node.Binding.Source, node.Binding.SemanticModel, newContracts, ImmutableArray<MdTag>.Empty)
        {
            Id = newId,
            SemanticModel = node.Binding.SemanticModel,
            Source = node.Binding.Source,
            Construct = new MdConstruct(
                node.Binding.SemanticModel,
                node.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContractsBuilder.ToImmutable())
        };
    }

    private IEnumerable<IEnumerable<DependencyNode>> AddBinding(
        MdSetup setup,
        MdBinding binding,
        CancellationToken cancellationToken)
    {
        var newSetup = setup with
        {
            Roots = ImmutableArray<MdRoot>.Empty,
            Bindings = ImmutableArray.Create(binding)
        };

        return SortNodes(_dependencyNodeBuilders.SelectMany(builder => builder.Build(newSetup, cancellationToken)))
            .Select(i => ImmutableArray.Create(i).AsEnumerable());
    }

    private static IEnumerable<DependencyNode> SortNodes(IEnumerable<DependencyNode> nodes) =>
        nodes
            .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
            .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
            .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility);

    private static IEnumerable<IEnumerable<DependencyNode>> SplitNodes(IEnumerable<DependencyNode> nodes)
    {
        var nodesGroups = 
            from node in nodes
            let binding = node.Binding
            group node by binding.Id
            into groupById
            orderby groupById.Key
            select SortNodes(groupById);
        
        var enumerators = nodesGroups.Select(i => i.GetEnumerator()).ToImmutableArray();
        try
        {
            List<DependencyNode> grp;
            do
            {
                grp = (from enumerator in enumerators where enumerator.MoveNext() select enumerator.Current).ToList();
                yield return grp;
            } while (grp.Any());
        }
        finally
        {
            foreach (var enumerator in enumerators)
            {
                enumerator.Dispose();
            }
        }
    }
    
    private readonly record struct ResolvingNode(DependencyNode Node, in ImmutableArray<Injection> Injections);
}