// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery

namespace Pure.DI.Core;

using Variation = IEnumerator<ProcessingNode>;

internal sealed class VariationalDependencyGraphBuilder(
    ILogger logger,
    IGlobalOptions globalOptions,
    Func<ITypeConstructor> typeConstructorFactory,
    IEnumerable<IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IVariator<ProcessingNode> variator,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IDependencyGraphBuilder graphBuilder,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, DependencyGraph?>
{
    public DependencyGraph? Build(MdSetup setup)
    {
        var dependencyNodeBuildContext = new DependencyNodeBuildContext(setup, typeConstructorFactory());
        var rawNodes = SortByPriority(dependencyNodeBuilders.SelectMany(builder => builder.Build(dependencyNodeBuildContext))).Reverse();
        var allNodes = new List<ProcessingNode>();
        var injections = new Dictionary<Injection, DependencyNode>();
        var allOverriddenInjections = new HashSet<Injection>();
        foreach (var node in rawNodes)
        {
            var contracts = contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag));
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
                        logger.CompileWarning($"{item.Key.ToString()} has been overridden.", item.Value.Binding.Source.GetLocation(), LogId.WarningOverriddenBinding);
                    }
                }
            }

            if (isRoot || contracts.Any())
            {
                allNodes.Add(new ProcessingNode(node, contracts));
            }
        }

        allNodes.Reverse();
        var variants = new LinkedList<Variation>(CreateVariants(allNodes));
        try
        {
            var maxIterations = globalOptions.MaxIterations;
            DependencyGraph? first = null;
            var maxAttempts = 0x2000;
            while (variator.TryGetNextVariants(variants, out var nodes))
            {
                if (maxAttempts-- == 0)
                {
                    throw new CompileErrorException("It is not possible to construct a dependency graph.", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }

                if (maxIterations-- <= 0)
                {
                    logger.CompileError($"The maximum number of iterations {globalOptions.MaxIterations.ToString()} was exceeded when building the optimal dependency graph. Try to specify the dependency graph more accurately.", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var newNodes = SortByPriority(graphBuilder.TryBuild(setup, nodes, out var dependencyGraph))
                    .Select(CreateProcessingNode)
                    .ToArray();

                if (newNodes.Any())
                {
                    var newVariants = CreateVariants(newNodes);
                    foreach (var newVariant in newVariants)
                    {
                        variants.AddFirst(newVariant);
                    }

                    continue;
                }

                if (dependencyGraph is { IsResolved: true })
                {
                    return dependencyGraph;
                }

                first ??= dependencyGraph;
                continue;

                ProcessingNode CreateProcessingNode(DependencyNode dependencyNode) => new(
                    dependencyNode,
                    contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, MdTag.ContextTag)));
            }

            return first;
        }
        finally
        {
            foreach (var variant in variants)
            {
                variant.Dispose();
            }
        }
    }

    [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
    private static IEnumerable<Variation> CreateVariants(IEnumerable<ProcessingNode> nodes) =>
        nodes.GroupBy(i => i.Node.Binding)
            .Select(i => new SafeEnumerator<ProcessingNode>(i.GetEnumerator()));

    private static IEnumerable<DependencyNode> SortByPriority(IEnumerable<DependencyNode> nodes) =>
        nodes.GroupBy(i => i.Binding)
            .OrderBy(i => i.Key.Id)
            .SelectMany(grp => grp
                .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
                .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
                .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility));
}