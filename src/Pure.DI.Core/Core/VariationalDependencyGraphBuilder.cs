// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery

namespace Pure.DI.Core;

using Variation=IEnumerator<IProcessingNode>;

sealed class VariationalDependencyGraphBuilder(
    ILogger logger,
    IGlobalProperties globalProperties,
    Func<ITypeConstructor> typeConstructorFactory,
    IEnumerable<IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IVariator<IProcessingNode> variator,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IDependencyGraphBuilder graphBuilder,
    Func<DependencyNode, ISet<Injection>, IProcessingNode> processingNodeFactory,
    IRegistryManager<int> registryManager,
    ILocationProvider locationProvider,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, DependencyGraph?>
{
    public DependencyGraph? Build(MdSetup setup)
    {
        var dependencyNodeBuildContext = new DependencyNodeBuildContext(setup, typeConstructorFactory());
        var rawNodes = SortByPriority(dependencyNodeBuilders.SelectMany(builder => builder.Build(dependencyNodeBuildContext))).Reverse();
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

                    if (node.Binding.SourceSetup.Kind != CompositionKind.Global)
                    {
                        logger.CompileWarning(
                            string.Format(Strings.Warning_Template_BindingHasBeenOverridden, item.Key),
                            ImmutableArray.Create(locationProvider.GetLocation(item.Value.Binding.Source)),
                            LogId.WarningOverriddenBinding);
                    }
                }
            }

            if (isRoot || contracts.Count > 0)
            {
                allNodes.Add(processingNodeFactory(node, contracts));
            }
        }

        allNodes.Reverse();
        var variants = new LinkedList<Variation>(CreateVariants(allNodes));
        try
        {
            var maxIterations = globalProperties.MaxIterations;
            var maxAttempts = 0x2000;
            DependencyGraph? dependencyGraph = null;
            while (variator.TryGetNextVariants(variants, out var nodes))
            {
                if (maxAttempts-- == 0)
                {
                    throw new CompileErrorException(
                        Strings.Error_CannotBuildDependencyGraph,
                        ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                        LogId.ErrorInvalidMetadata);
                }

                if (maxIterations-- <= 0)
                {
                    logger.CompileError(
                        string.Format(Strings.Error_Template_MaximumNumberOfIterations, globalProperties.MaxIterations),
                        ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                        LogId.ErrorInvalidMetadata);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var newNodes = SortByPriority(graphBuilder.TryBuild(setup, nodes, out var graph))
                    .Select(CreateProcessingNode)
                    .ToArray();

                if (newNodes.Length > 0)
                {
                    var newVariants = CreateVariants(newNodes);
                    foreach (var newVariant in newVariants)
                    {
                        variants.AddFirst(newVariant);
                    }

                    continue;
                }

                if (graph is null)
                {
                    continue;
                }

                dependencyGraph = new DependencyGraph(setup, graph);
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

                continue;

                IProcessingNode CreateProcessingNode(DependencyNode dependencyNode) =>
                    processingNodeFactory(
                        dependencyNode,
                        contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, MdTag.ContextTag, MdTag.AnyTag)));
            }

            return dependencyGraph;
        }
        finally
        {
            foreach (var variant in variants)
            {
                variant.Dispose();
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
                registryManager.Register(setup, id);
            }
        }

        registryManager.Register(setup, binding.Id);
    }

    [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
    private static IEnumerable<Variation> CreateVariants(IEnumerable<IProcessingNode> nodes) =>
        nodes.GroupBy(i => i.Node.Binding)
            .Select(i => new SafeEnumerator<IProcessingNode>(i.GetEnumerator()));

    private static IEnumerable<DependencyNode> SortByPriority(IEnumerable<DependencyNode> nodes) =>
        nodes.GroupBy(i => i.Binding)
            .OrderBy(i => i.Key.Id)
            .SelectMany(grp => grp
                .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
                .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
                .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility));
}