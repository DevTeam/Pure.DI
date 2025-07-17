// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable IdentifierTypo

namespace Pure.DI.Core;

using static Tag;
using Dependency = Dependency;
using DependencyNode = DependencyNode;
using Injection=Injection;

sealed class DependencyGraphBuilder(
    INodesFactory nodesFactory,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IMarker marker,
    IBindingsFactory bindingsFactory,
    Func<ITypeConstructor> typeConstructorFactory,
    IFilter filter,
    ICache<INamedTypeSymbol, MdConstructKind> constructKinds,
    ISymbolNames symbolNames,
    Func<DependencyNode, ISet<Injection>, IProcessingNode> processingNodeFactory,
    [Tag(Overrider)] IGraphRewriter graphOverrider,
    [Tag(Cleaner)] IGraphRewriter graphCleaner,
    ILocationProvider locationProvider,
    CancellationToken cancellationToken)
    : IDependencyGraphBuilder
{
    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        out IGraph<DependencyNode, Dependency>? graph)
    {
        graph = null;
        var maxId = 0;
        var map = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var contextMap = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var queue = new Queue<IProcessingNode>();
        foreach (var processingNode in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            else
            {
                queue.Enqueue(processingNode);
            }
        }

        var accumulators = new Dictionary<ITypeSymbol, List<MdAccumulator>>(SymbolEqualityComparer.Default);
        foreach (var accumulator in setup.Accumulators)
        {
            if (!accumulators.TryGetValue(accumulator.AccumulatorType, out var accs))
            {
                accs = [];
                accumulators.Add(accumulator.AccumulatorType, accs);
            }

            accs.Add(accumulator);
        }

        var processedInjection = new HashSet<Injection>();
        var processed = new HashSet<IProcessingNode>();
        var notProcessed = new HashSet<IProcessingNode>();
        var edgesMap = new Dictionary<IProcessingNode, List<Dependency>>();
        var counter = 0;
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            cancellationToken.ThrowIfCancellationRequested();
            if (counter++ > Const.MaxIterationsCount)
            {
                throw new CompileErrorException(
                    string.Format(Strings.Error_Template_TooLargeComposition, counter),
                    ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                    LogId.ErrorInvalidMetadata);
            }

            var targetNode = node.Node;
            var isProcessed = true;
            if (targetNode.Error is not null)
            {
                notProcessed.Add(node);
                continue;
            }

            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue, _) in node.Injections)
            {
                if (!processedInjection.Add(injection))
                {
                    continue;
                }

                if (map.TryGetValue(injection, out var sourceNode))
                {
                    if (!marker.IsMarkerBased(setup, sourceNode.Type))
                    {
                        queue.Enqueue(CreateNewProcessingNode(injection.Tag, sourceNode));
                        continue;
                    }
                }

                var typeConstructor = typeConstructorFactory();
                if (accumulators.TryGetValue(injection.Type, out var accs))
                {
                    var accumulatorBinding = bindingsFactory.CreateAccumulatorBinding(
                        setup,
                        targetNode,
                        ref maxId,
                        accs,
                        hasExplicitDefaultValue,
                        explicitDefaultValue);

                    return nodesFactory.CreateNodes(setup, typeConstructor, accumulatorBinding);
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol namedTypeSymbol
                        when namedTypeSymbol.IsGenericType || marker.IsMarkerBased(setup, namedTypeSymbol):
                    {
                        var isDone = false;
                        foreach (var item in map.OrderByDescending(i => i.Value.Binding.Id))
                        {
                            if (item.Key.Type is not INamedTypeSymbol nextNamedTypeSymbol)
                            {
                                continue;
                            }

                            if (nextNamedTypeSymbol.IsGenericType != namedTypeSymbol.IsGenericType)
                            {
                                continue;
                            }

                            if (!Injection.EqualTags(injection.Tag, item.Key.Tag))
                            {
                                continue;
                            }

                            if (!typeConstructor.TryBind(setup, item.Key.Type, injection.Type))
                            {
                                continue;
                            }

                            sourceNode = item.Value;
                            var contextTag = GetContextTag(injection, sourceNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            var genericBinding = bindingsFactory.CreateGenericBinding(
                                setup,
                                newInjection,
                                sourceNode,
                                typeConstructor,
                                ++maxId);

                            var genericNode = nodesFactory.CreateNodes(setup, typeConstructor, genericBinding).Single(i => i.Variation == sourceNode.Variation);
                            UpdateMap(newInjection, genericNode);
                            queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, genericNode));
                            foreach (var contract in genericBinding.Contracts.Where(i => i.ContractType is not null))
                            {
                                foreach (var tag in contract.Tags.Select(i => i.Value).DefaultIfEmpty(null))
                                {
                                    newInjection = new Injection(InjectionKind.Contract, contract.ContractType!, contextTag ?? tag, injection.Locations);
                                    UpdateMap(newInjection, genericNode);
                                }
                            }

                            isDone = true;
                            break;
                        }

                        if (isDone)
                        {
                            continue;
                        }

                        // Construct
                        if (namedTypeSymbol.TypeArguments is [{} constructType])
                        {
                            var constructKind = GetConstructKind(namedTypeSymbol);
                            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                            switch (constructKind)
                            {
                                case MdConstructKind.None:
                                    break;

                                default:
                                    var lifetime = constructKind == MdConstructKind.Enumerable ? Lifetime.PerBlock : Lifetime.Transient;
                                    var constructBinding = bindingsFactory.CreateConstructBinding(
                                        setup,
                                        targetNode,
                                        injection,
                                        constructType,
                                        lifetime,
                                        typeConstructor,
                                        ++maxId,
                                        constructKind);

                                    foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, constructBinding))
                                    {
                                        var contextTag = GetContextTag(injection, newNode);
                                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                                        UpdateMap(newInjection, newNode);
                                        var processingNode = CreateNewProcessingNode(newInjection.Tag, newNode);
                                        queue.Enqueue(processingNode);
                                    }

                                    continue;
                            }
                        }

                        // OnCannotResolve
                        if (TryCreateOnCannotResolve(setup, typeConstructor, targetNode, injection, ref maxId, map, processed))
                        {
                            continue;
                        }

                        break;
                    }

                    // Array construct
                    case IArrayTypeSymbol arrayType:
                    {
                        var arrayBinding = bindingsFactory.CreateConstructBinding(
                            setup,
                            targetNode,
                            injection,
                            arrayType.ElementType,
                            Lifetime.Transient,
                            typeConstructor,
                            ++maxId,
                            MdConstructKind.Array);

                        foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, arrayBinding))
                        {
                            var contextTag = GetContextTag(injection, newNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            UpdateMap(newInjection, newNode);
                            queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, newNode));
                        }

                        continue;
                    }
                }

                // ExplicitDefaultValue
                if (hasExplicitDefaultValue)
                {
                    var explicitDefaultBinding = bindingsFactory.CreateConstructBinding(
                        setup,
                        targetNode,
                        injection,
                        injection.Type,
                        Lifetime.Transient,
                        typeConstructor,
                        ++maxId,
                        MdConstructKind.ExplicitDefaultValue,
                        null,
                        hasExplicitDefaultValue, explicitDefaultValue);

                    var newSourceNodes = nodesFactory.CreateNodes(setup, typeConstructor, explicitDefaultBinding);
                    foreach (var newNode in newSourceNodes)
                    {
                        if (!edgesMap.TryGetValue(node, out var edges))
                        {
                            edges = [];
                            edgesMap.Add(node, edges);
                        }

                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        edges.Add(new Dependency(true, newNode, newInjection, targetNode));
                    }

                    continue;
                }

                if (symbolNames.GetName(injection.Type) == setup.Name.FullName)
                {
                    // Composition
                    var compositionBinding = bindingsFactory.CreateConstructBinding(
                        setup,
                        targetNode,
                        injection,
                        injection.Type,
                        Lifetime.Transient,
                        typeConstructor,
                        ++maxId,
                        MdConstructKind.Composition);

                    foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, compositionBinding))
                    {
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        UpdateMap(newInjection, newNode);
                        queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, newNode));
                    }

                    continue;
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: Microsoft.CodeAnalysis.SpecialType.None })
                {
                    var autoBinding = bindingsFactory.CreateAutoBinding(setup, targetNode, injection, typeConstructor, ++maxId);
                    return nodesFactory.CreateNodes(setup, typeConstructor, autoBinding).ToList();
                }

                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, typeConstructor, targetNode, injection, ref maxId, map, processed))
                {
                    continue;
                }

                // Not processed
                notProcessed.Add(node);
                isProcessed = false;
            }

            if (isProcessed)
            {
                processed.Add(node);
            }
        }

        foreach (var item in contextMap)
        {
            map[item.Key] = item.Value;
        }

        foreach (var key in map.Keys.Where(i => ReferenceEquals(i.Tag, MdTag.ContextTag)).ToList())
        {
            map.Remove(key);
        }

        var entries = new List<GraphEntry<DependencyNode, Dependency>>(processed.Count);
        foreach (var node in processed.Concat(notProcessed))
        {
            if (!edgesMap.TryGetValue(node, out var edges))
            {
                edges = [];
                edgesMap.Add(node, edges);
            }

            foreach (var injection in node.Injections)
            {
                var dependency = map.TryGetValue(injection.Injection, out var sourceNode) && sourceNode.Error is null
                    ? new Dependency(true, sourceNode, injection.Injection, node.Node, injection.Position)
                    : new Dependency(false, new DependencyNode(0, node.Node.Binding, node.Node.TypeConstructor), injection.Injection, node.Node, injection.Position, sourceNode?.Error);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges));
        }

        graph = new Graph<DependencyNode, Dependency>(entries);
        var lastId = maxId;
        graph = graphOverrider.Rewrite(setup, graph, ref maxId);
        // Has overrides
        if (lastId != maxId)
        {
            graph = graphCleaner.Rewrite(setup, graph, ref maxId);
        }

        return ImmutableArray<DependencyNode>.Empty;

        void UpdateMap(Injection injection, DependencyNode node)
        {
            if (node.Factory is { Source.HasContextTag: true })
            {
                contextMap[injection] = node;
            }
            else
            {
                map[injection] = node;
            }
        }
    }

    private MdConstructKind GetConstructKind(INamedTypeSymbol geneticType)
    {
        var unboundGenericType = geneticType.ConstructUnboundGenericType();
        return constructKinds.Get(unboundGenericType, type => symbolNames.GetGlobalName(type) switch
        {
            Names.SpanTypeName => MdConstructKind.Span,
            Names.ReadOnlySpanTypeName => MdConstructKind.Span,
            Names.IEnumerableTypeName => MdConstructKind.Enumerable,
            Names.IAsyncEnumerableTypeName => MdConstructKind.AsyncEnumerable,
            _ => MdConstructKind.None
        });
    }

    private bool TryCreateOnCannotResolve(
        MdSetup setup,
        ITypeConstructor typeConstructor,
        DependencyNode ownerNode,
        Injection unresolvedInjection,
        ref int maxId,
        IDictionary<Injection, DependencyNode> map,
        ISet<IProcessingNode> processed)
    {
        if (setup.Hints.IsOnCannotResolveEnabled)
        {
            string GetContractName() => symbolNames.GetName(unresolvedInjection.Type);
            string GetTagName() => unresolvedInjection.Tag.ValueToString();
            string GetLifetimeName() => ownerNode.Lifetime.ValueToString();
            if (filter.IsMeet(
                    setup,
                    (Hint.OnCannotResolveContractTypeNameRegularExpression, Hint.OnCannotResolveContractTypeNameWildcard, GetContractName),
                    (Hint.OnCannotResolveTagRegularExpression, Hint.OnCannotResolveTagWildcard, GetTagName),
                    (Hint.OnCannotResolveLifetimeRegularExpression, Hint.OnCannotResolveLifetimeWildcard, GetLifetimeName)))
            {
                var onCannotResolveBinding = bindingsFactory.CreateConstructBinding(
                    setup,
                    ownerNode,
                    unresolvedInjection,
                    unresolvedInjection.Type,
                    Lifetime.Transient,
                    typeConstructor,
                    ++maxId,
                    MdConstructKind.OnCannotResolve,
                    unresolvedInjection.Tag);

                var onCannotResolveNodes = nodesFactory.CreateNodes(setup, typeConstructor, onCannotResolveBinding);
                foreach (var onCannotResolveNode in onCannotResolveNodes)
                {
                    map[unresolvedInjection] = onCannotResolveNode;
                    processed.Add(CreateNewProcessingNode(unresolvedInjection.Tag, onCannotResolveNode));
                    return true;
                }
            }
        }

        return false;
    }

    private IProcessingNode CreateNewProcessingNode(object? contextTag, DependencyNode dependencyNode)
    {
        var contracts = contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, contextTag, contextTag));
        return processingNodeFactory(dependencyNode, contracts);
    }

    private static object? GetContextTag(Injection injection, DependencyNode node) =>
        node.Factory is { Source.HasContextTag: true } ? injection.Tag : null;
}