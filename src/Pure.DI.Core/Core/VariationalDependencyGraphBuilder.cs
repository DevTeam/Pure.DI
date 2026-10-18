// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery

// ReSharper disable UseCollectionExpression
#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core;

using Options = SetOfOptions<IProcessingNode>;

sealed class VariationalDependencyGraphBuilder(
    ILogger logger,
    IGlobalProperties globalProperties,
    Func<ITypeConstructor> typeConstructorFactory,
    IEnumerable<IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IVariator<IProcessingNode> nodeVariator,
    IFastBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IBuilder<GraphBuildContext, IEnumerable<DependencyNode>> graphBuilder,
    IFastBuilder<ProcessingNodeContext, IProcessingNode> processingNodeBuilder,
    IRegistryManager<int> bindingsRegistryManager,
    ILocationProvider locationProvider,
    IDependencyNodePrioritizer dependencyNodePrioritizer,
    IInjectionComparer injectionComparer,
    ITypeSymbolComparer typeSymbolComparer,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, DependencyGraph?>
{
    public DependencyGraph? Build(MdSetup setup)
    {
        var dependencyNodeBuildContext = new DependencyNodeBuildContext(setup, setup, typeConstructorFactory());
        var rawNodes = dependencyNodePrioritizer.SortByPriority(dependencyNodeBuilders.SelectMany(builder => builder.Build(dependencyNodeBuildContext))).Reverse();
        var allNodes = new List<IProcessingNode>();
        var injections = new Dictionary<Injection, DependencyNode>(injectionComparer);
        var allOverriddenInjections = new HashSet<Injection>(injectionComparer);
        foreach (var node in rawNodes)
        {
            var contracts = contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag, MdTag.AnyTag));
            var isRoot = node.Root is not null;
            if (!isRoot)
            {
                var overriddenInjections = new List<KeyValuePair<Injection, DependencyNode>>();
                foreach (var exposedInjection in contracts)
                {
                    if (!injections.TryGetValue(exposedInjection, out var currentNode) || currentNode.Binding == node.Binding)
                    {
                        injections[exposedInjection] = node;
                    }
                    else
                    {
                        overriddenInjections.Add(new KeyValuePair<Injection, DependencyNode>(exposedInjection, currentNode));
                    }
                }

                foreach (var item in overriddenInjections)
                {
                    contracts.Remove(item.Key);
                    if (!allOverriddenInjections.Add(item.Key))
                    {
                        continue;
                    }

                    // ReSharper disable once InvertIf
                    if (node.Binding.SourceSetup.Kind != CompositionKind.Global)
                    {
                        var warningSource = node.Binding.Arg is { IsSetupContext: true }
                            ? node.Binding.Source
                            : item.Value.Binding.Source;

                        logger.CompileWarning(
                            LogMessage.Format(
                                nameof(Strings.Warning_Template_BindingHasBeenOverridden),
                                Strings.Warning_Template_BindingHasBeenOverridden,
                                item.Key),
                            ImmutableArray.Create(locationProvider.GetLocation(warningSource)),
                            LogId.WarningOverriddenBinding);
                    }
                }
            }

            if (isRoot || contracts.Count > 0)
            {
                allNodes.Add(processingNodeBuilder.Build(new ProcessingNodeContext(node, MdTag.ContextTag, contracts)));
            }
        }

        allNodes.Reverse();
        var setsOfOptions = new LinkedList<Options>(CreateOptions(allNodes));
        var maxIterations = globalProperties.MaxVariations;
        var buildAttempts = 0;
        DependencyGraph? dependencyGraph = null;
        var visitedStates = new HashSet<ImmutableArray<long>>(VariationStateComparer.Shared);

        var accumulators = setup.Accumulators
            .GroupBy(acc => acc.AccumulatorType, new SymbolTypeComparer(typeSymbolComparer))
            .ToImmutableDictionary(i => i.Key, i => i.ToImmutableArray(), new SymbolTypeComparer(typeSymbolComparer));

        var buildCtx = new GraphBuildContext(
            setup,
            ImmutableArray<IProcessingNode>.Empty,
            accumulators);

        var hasNodes = TryGetNextUnvisited(out var nodes);
        while (hasNodes)
        {
            if (maxIterations-- <= 0)
            {
                logger.CompileError(
                    LogMessage.Format(
                        nameof(Strings.Error_Template_MaximumNumberOfIterations),
                        Strings.Error_Template_MaximumNumberOfIterations,
                        globalProperties.MaxVariations),
                    ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                    LogId.ErrorMaximumNumberOfIterations);

                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            buildAttempts++;

            buildCtx = buildCtx with { Nodes = nodes };
            var newNodes = graphBuilder.Build(buildCtx).ToList();
            var graph = buildCtx.Graph;
            if (graph is not null)
            {
                dependencyGraph = new DependencyGraph(setup, graph) { BuildAttempts = buildAttempts };
            }

            if (dependencyGraph is { IsResolved: true })
            {
                foreach (var dependency in dependencyGraph.Graph.Edges)
                {
                    RegisterNode(setup, dependency.Target);
                    RegisterNode(setup, dependency.Source);
                }

                foreach (var node in dependencyGraph.Graph.Vertices)
                {
                    RegisterNode(setup, node);
                }

                return dependencyGraph;
            }

            if (newNodes.Count == 0)
            {
                // An unresolved dependency can only be affected by the selected variants of its owner
                // and the consumers on the path to a root. Try those variants before resuming the full
                // Cartesian search. The exhaustive search below remains the correctness fallback.
                var prioritizedSetsOfOptions = dependencyGraph is not null
                    ? GetPrioritizedSetsOfOptions(dependencyGraph.Graph, setsOfOptions)
                    : [];
                if (prioritizedSetsOfOptions.Count > 0
                    && TryGetNextPrioritizedUnvisited(prioritizedSetsOfOptions, out nodes))
                {
                    continue;
                }

                ResetOptions();
                hasNodes = TryGetNextUnvisited(out nodes);
                continue;
            }

            ResetOptions();

            var newProcessingNodes = dependencyNodePrioritizer.SortByPriority(newNodes).Select(CreateProcessingNode);
            foreach (var newOption in CreateOptions(newProcessingNodes))
            {
                setsOfOptions.AddFirst(newOption);
            }

            // A larger node catalog can make a previously rejected state resolvable, so states from
            // the previous catalog revision cannot be used as negative results.
            visitedStates.Clear();
            hasNodes = TryGetNextUnvisited(out nodes);

            continue;

            IProcessingNode CreateProcessingNode(DependencyNode dependencyNode)
            {
                var processingNode = processingNodeBuilder.Build(new ProcessingNodeContext(dependencyNode, MdTag.ContextTag, contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, MdTag.ContextTag, MdTag.AnyTag))));
                allNodes.Add(processingNode);
                return processingNode;
            }
        }

        return dependencyGraph;

        bool TryGetNextUnvisited(out ImmutableArray<IProcessingNode> nextNodes)
        {
            while (nodeVariator.TryGetNext(setsOfOptions, out nextNodes))
            {
                if (visitedStates.Add(CreateVariationState(nextNodes)))
                {
                    return true;
                }
            }

            nextNodes = default;
            return false;
        }

        bool TryGetNextPrioritizedUnvisited(
            IReadOnlyCollection<ISetOfOptions<IProcessingNode>> prioritizedSetsOfOptions,
            out ImmutableArray<IProcessingNode> nextNodes)
        {
            while (nodeVariator.TryGetNext(setsOfOptions, prioritizedSetsOfOptions, out nextNodes))
            {
                if (visitedStates.Add(CreateVariationState(nextNodes)))
                {
                    return true;
                }
            }

            nextNodes = default;
            return false;
        }

        void ResetOptions()
        {
            foreach (var options in setsOfOptions)
            {
                options.Reset();
            }
        }
    }

    private void RegisterNode(MdSetup setup, DependencyNode node)
    {
        var binding = node.Binding;
        if (!binding.OriginalIds.IsDefaultOrEmpty)
        {
            foreach (var id in binding.OriginalIds)
            {
                bindingsRegistryManager.Register(setup, id);
            }
        }

        bindingsRegistryManager.Register(setup, binding.Id);
    }

    [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
    private static IEnumerable<Options> CreateOptions(IEnumerable<IProcessingNode> nodes) =>
        nodes.GroupBy(i => i.Node.Binding)
            .Select(i => new Options(i.ToList()));

    private static IReadOnlyCollection<ISetOfOptions<IProcessingNode>> GetPrioritizedSetsOfOptions(
        IGraph<DependencyNode, Dependency> graph,
        IEnumerable<Options> setsOfOptions)
    {
        var optionsByBindingId = new Dictionary<int, Options>();
        var orderedOptions = new List<(Options Options, int BindingId)>();
        foreach (var options in setsOfOptions)
        {
            if (options is { IsStarted: true, Current: {} current })
            {
                var bindingId = current.Node.Binding.Id;
                orderedOptions.Add((options, bindingId));
                if (!optionsByBindingId.ContainsKey(bindingId))
                {
                    optionsByBindingId.Add(bindingId, options);
                }
            }
        }
        var prioritizedBindingIds = new HashSet<int>();
        var visitedNodes = new HashSet<DependencyNode>();
        var queue = new Queue<DependencyNode>(graph.Edges.Where(i => !i.IsResolved).Select(i => i.Target));
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visitedNodes.Add(node))
            {
                continue;
            }

            var bindingId = node.Binding.Id;
            if (optionsByBindingId.ContainsKey(bindingId))
            {
                prioritizedBindingIds.Add(bindingId);
            }

            if (!graph.TryGetOutEdges(node, out var consumers))
            {
                continue;
            }

            foreach (var consumer in consumers)
            {
                queue.Enqueue(consumer.Target);
            }
        }

        // Keep the original mixed-radix order among the options that can affect the conflict.
        return orderedOptions
            .Where(i => prioritizedBindingIds.Contains(i.BindingId))
            .Select(i => (ISetOfOptions<IProcessingNode>)i.Options)
            .ToList();
    }

    private static ImmutableArray<long> CreateVariationState(IEnumerable<IProcessingNode> nodes) =>
        nodes.Select(i => ((long)i.Node.Binding.Id << 32) | (uint)i.Node.Variation).ToImmutableArray();

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    private sealed class SymbolTypeComparer(ITypeSymbolComparer typeSymbolComparer) : IEqualityComparer<ISymbol>
    {
        public bool Equals(ISymbol? x, ISymbol? y) =>
            x is ITypeSymbol type
            && y is ITypeSymbol otherType
            && typeSymbolComparer.DependencyEquals(type, otherType);

        public int GetHashCode(ISymbol obj) =>
            obj is ITypeSymbol type ? typeSymbolComparer.GetDependencyHashCode(type) : obj.GetHashCode();
    }

    private sealed class VariationStateComparer : IEqualityComparer<ImmutableArray<long>>
    {
        public static readonly VariationStateComparer Shared = new();

        public bool Equals(ImmutableArray<long> x, ImmutableArray<long> y) => x.AsSpan().SequenceEqual(y.AsSpan());

        public int GetHashCode(ImmutableArray<long> state)
        {
            unchecked
            {
                var hashCode = 0;
                foreach (var item in state)
                {
                    hashCode = hashCode * 397 ^ item.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}
