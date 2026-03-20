// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery

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
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, DependencyGraph?>
{
    public DependencyGraph? Build(MdSetup setup)
    {
        var dependencyNodeBuildContext = new DependencyNodeBuildContext(setup, setup, typeConstructorFactory());
        var rawNodes = dependencyNodePrioritizer.SortByPriority(dependencyNodeBuilders.SelectMany(builder => builder.Build(dependencyNodeBuildContext))).Reverse();
        var allNodes = new List<IProcessingNode>();
        var injections = new Dictionary<Injection, DependencyNode>();
        var allOverriddenInjections = new HashSet<Injection>();
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
        DependencyGraph? dependencyGraph = null;

        var accumulators = setup.Accumulators
            .GroupBy(acc => acc.AccumulatorType, SymbolEqualityComparer.Default)
            .ToImmutableDictionary(i => i.Key!, i => i.ToImmutableArray(), SymbolEqualityComparer.Default);

        var buildCtx = new GraphBuildContext(
            setup,
            ImmutableArray<IProcessingNode>.Empty,
            accumulators);

        while (nodeVariator.TryGetNext(setsOfOptions, out var nodes))
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

            buildCtx = buildCtx with { Nodes = nodes };
            var newNodes = graphBuilder.Build(buildCtx).ToList();
            var graph = buildCtx.Graph;
            if (graph is not null)
            {
                dependencyGraph = new DependencyGraph(setup, graph);
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
                continue;
            }

            foreach (var options in setsOfOptions)
            {
                options.Reset();
            }

            var newProcessingNodes = dependencyNodePrioritizer.SortByPriority(newNodes).Select(CreateProcessingNode);
            foreach (var newOption in CreateOptions(newProcessingNodes))
            {
                setsOfOptions.AddFirst(newOption);
            }

            continue;

            IProcessingNode CreateProcessingNode(DependencyNode dependencyNode)
            {
                var processingNode = processingNodeBuilder.Build(new ProcessingNodeContext(dependencyNode, MdTag.ContextTag, contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, MdTag.ContextTag, MdTag.AnyTag))));
                allNodes.Add(processingNode);
                return processingNode;
            }
        }

        return dependencyGraph;
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
}
