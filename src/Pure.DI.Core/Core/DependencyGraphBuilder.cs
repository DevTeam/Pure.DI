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
    IMarker marker,
    IBindingsFactory bindingsFactory,
    Func<ITypeConstructor> typeConstructorFactory,
    IFilter filter,
    ICache<INamedTypeSymbol, MdConstructKind> constructKinds,
    ISymbolNames symbolNames,
    IBuilder<ProcessingNodeContext, IProcessingNode> processingNodeBuilder,
    [Tag(Overrider)] IGraphRewriter graphOverrider,
    [Tag(Cleaner)] IGraphRewriter graphCleaner,
    ILocationProvider locationProvider,
    ITypeResolver typeResolver,
    CancellationToken cancellationToken)
    : IDependencyGraphBuilder
{
    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        ICache<ProcessingNodeKey, IProcessingNode> nodesCache,
        out IGraph<DependencyNode, Dependency>? graph)
    {
        graph = null;
        var maxBindingId = 0;
        var map = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var contextMap = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var queue = new Queue<IProcessingNode>();
        foreach (var processingNode in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = processingNode.Node;
            if (node.Binding.Id > maxBindingId)
            {
                maxBindingId = node.Binding.Id;
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
        var handledInjections = new Dictionary<IProcessingNode, HashSet<(Injection Injection, int? Position)>>();
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
                    LogId.ErrorTooLargeComposition,
                    nameof(Strings.Error_Template_TooLargeComposition));
            }

            var targetNode = node.Node;
            var isProcessed = true;
            if (targetNode.Error is not null)
            {
                notProcessed.Add(node);
                continue;
            }

            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue, position) in node.Injections)
            {
                var hasSourceNode = map.TryGetValue(injection, out var sourceNode);
                var bypassSelfFactoryOverride = hasSourceNode
                                                && sourceNode!.Binding.Id == targetNode.Binding.Id
                                                && injection.Kind == InjectionKind.FactoryInjection
                                                && targetNode.Factory is not null;

                if (bypassSelfFactoryOverride
                    && injection.Type is { IsAbstract: false, TypeKind: not TypeKind.Delegate, SpecialType: Microsoft.CodeAnalysis.SpecialType.None })
                {
                    var autoTypeConstructor = typeConstructorFactory();
                    var autoBinding = bindingsFactory.CreateAutoBinding(setup, targetNode, injection, autoTypeConstructor, ++maxBindingId);
                    foreach (var newNode in nodesFactory.CreateNodes(setup, autoTypeConstructor, autoBinding))
                    {
                        if (!edgesMap.TryGetValue(node, out var edges))
                        {
                            edges = [];
                            edgesMap.Add(node, edges);
                        }

                        MarkHandledInjection(node, injection, position);
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        edges.Add(new Dependency(true, newNode, newInjection, targetNode, position));
                        queue.Enqueue(CreateNewProcessingNode(nodesCache, newInjection.Tag, newNode));
                    }

                    continue;
                }

                if (!processedInjection.Add(injection))
                {
                    continue;
                }

                if (hasSourceNode)
                {
                    if (!marker.IsMarkerBased(setup, sourceNode!.Type))
                    {
                        queue.Enqueue(CreateNewProcessingNode(nodesCache, injection.Tag, sourceNode));
                        continue;
                    }
                }

                var typeConstructor = typeConstructorFactory();
                if (accumulators.TryGetValue(injection.Type, out var accs))
                {
                    var accumulatorBinding = bindingsFactory.CreateAccumulatorBinding(
                        setup,
                        targetNode,
                        ref maxBindingId,
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
                                ++maxBindingId);

                            var genericNodes = nodesFactory.CreateNodes(setup, typeConstructor, genericBinding).ToList();
                            var genericNode = genericNodes.FirstOrDefault(i => i.Variation == sourceNode.Variation);
                            if (genericNode is not null)
                            {
                                UpdateMap(newInjection, genericNode);
                                queue.Enqueue(CreateNewProcessingNode(nodesCache, newInjection.Tag, genericNode));
                                foreach (var contract in genericBinding.Contracts.Where(i => i.ContractType is not null))
                                {
                                    foreach (var tag in contract.Tags.Select(i => i.Value).DefaultIfEmpty(null))
                                    {
                                        newInjection = new Injection(InjectionKind.Contract, RefKind.None, contract.ContractType!, contextTag ?? tag, injection.Locations);
                                        UpdateMap(newInjection, genericNode);
                                    }
                                }

                                isDone = true;
                                break;
                            }

                            if (genericNodes.Count > 0)
                            {
                                return genericNodes;
                            }
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
                                        ++maxBindingId,
                                        constructKind);

                                    foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, constructBinding))
                                    {
                                        var contextTag = GetContextTag(injection, newNode);
                                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                                        UpdateMap(newInjection, newNode);
                                        var processingNode = CreateNewProcessingNode(nodesCache, newInjection.Tag, newNode);
                                        queue.Enqueue(processingNode);
                                    }

                                    continue;
                            }
                        }

                        // OnCannotResolve
                        if (TryCreateOnCannotResolve(nodesCache, setup, typeConstructor, targetNode, injection, ref maxBindingId, map, processed))
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
                            ++maxBindingId,
                            MdConstructKind.Array);

                        foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, arrayBinding))
                        {
                            var contextTag = GetContextTag(injection, newNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            UpdateMap(newInjection, newNode);
                            queue.Enqueue(CreateNewProcessingNode(nodesCache, newInjection.Tag, newNode));
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
                        ++maxBindingId,
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

                        MarkHandledInjection(node, injection, position);
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        edges.Add(new Dependency(true, newNode, newInjection, targetNode, position));
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
                        ++maxBindingId,
                        MdConstructKind.Composition);

                    foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, compositionBinding))
                    {
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        UpdateMap(newInjection, newNode);
                        queue.Enqueue(CreateNewProcessingNode(nodesCache, newInjection.Tag, newNode));
                    }

                    continue;
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: Microsoft.CodeAnalysis.SpecialType.None })
                {
                    var autoBinding = bindingsFactory.CreateAutoBinding(setup, targetNode, injection, typeConstructor, ++maxBindingId);
                    var disableAutoBinding = false;
                    if (setup.Hints.DisableAutoBinding)
                    {
                        string GetTypeName() => typeResolver.Resolve(setup, autoBinding.Type).Name;
                        string GetLifetimeName() => targetNode.Lifetime.ValueToString();
                        if (filter.IsMeet(
                                setup,
                                (Hint.DisableAutoBindingImplementationTypeNameRegularExpression, Hint.DisableAutoBindingImplementationTypeNameWildcard, GetTypeName),
                                (Hint.DisableAutoBindingLifetimeRegularExpression, Hint.DisableAutoBindingLifetimeWildcard, GetLifetimeName)))
                        {
                            disableAutoBinding = true;
                        }
                    }

                    if (!disableAutoBinding)
                    {
                        return nodesFactory.CreateNodes(setup, typeConstructor, autoBinding).ToList();
                    }
                }

                // OnCannotResolve
                if (TryCreateOnCannotResolve(nodesCache, setup, typeConstructor, targetNode, injection, ref maxBindingId, map, processed))
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
                if (IsInjectionHandled(node, injection.Injection, injection.Position))
                {
                    continue;
                }

                var dependency = map.TryGetValue(injection.Injection, out var sourceNode) && sourceNode.Error is null
                    ? new Dependency(true, sourceNode, injection.Injection, node.Node, injection.Position)
                    : new Dependency(false, new DependencyNode(0, node.Node.Binding, node.Node.TypeConstructor), injection.Injection, node.Node, injection.Position, sourceNode?.Error);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges));
        }

        graph = new Graph<DependencyNode, Dependency>(entries);
        var lastId = maxBindingId;
        graph = graphOverrider.Rewrite(setup, graph, ref maxBindingId);
        // Has overrides
        if (lastId != maxBindingId)
        {
            graph = graphCleaner.Rewrite(setup, graph, ref maxBindingId);
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

        void MarkHandledInjection(IProcessingNode processingNode, Injection injection, int? position)
        {
            if (!handledInjections.TryGetValue(processingNode, out var injections))
            {
                injections = [];
                handledInjections.Add(processingNode, injections);
            }

            injections.Add((injection, position));
        }

        bool IsInjectionHandled(IProcessingNode processingNode, Injection injection, int? position) =>
            handledInjections.TryGetValue(processingNode, out var injections) && injections.Contains((injection, position));
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
        ICache<ProcessingNodeKey, IProcessingNode> nodesCache,
        MdSetup setup,
        ITypeConstructor typeConstructor,
        DependencyNode ownerNode,
        Injection unresolvedInjection,
        ref int bindingId,
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
                    ++bindingId,
                    MdConstructKind.OnCannotResolve,
                    unresolvedInjection.Tag);

                var onCannotResolveNodes = nodesFactory.CreateNodes(setup, typeConstructor, onCannotResolveBinding);
                foreach (var onCannotResolveNode in onCannotResolveNodes)
                {
                    map[unresolvedInjection] = onCannotResolveNode;
                    processed.Add(CreateNewProcessingNode(nodesCache, unresolvedInjection.Tag, onCannotResolveNode));
                    return true;
                }
            }
        }

        return false;
    }

    private IProcessingNode CreateNewProcessingNode(ICache<ProcessingNodeKey, IProcessingNode> nodesCache, object? contextTag, DependencyNode dependencyNode) =>
        processingNodeBuilder.Build(new ProcessingNodeContext(nodesCache, dependencyNode, contextTag));

    private static object? GetContextTag(Injection injection, DependencyNode node) =>
        node.Factory is { Source.HasContextTag: true } ? injection.Tag : null;
}