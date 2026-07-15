// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable IdentifierTypo
#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

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
    ICache<DependencyGraphBuilder.ConstructKindKey, MdConstructKind> constructKinds,
    ISymbolNames symbolNames,
    IFastBuilder<ProcessingNodeContext, IProcessingNode> processingNodeBuilder,
    [Tag(Overrider)] IGraphRewriter graphOverrider,
    [Tag(Cleaner)] IGraphRewriter graphCleaner,
    ILocationProvider locationProvider,
    ITypeResolver typeResolver,
    ITypes types,
    IDependencyNodePrioritizer dependencyNodePrioritizer,
    IGlobalProperties globalProperties,
    IInjectionComparer injectionComparer,
    ITypeSymbolComparer typeSymbolComparer,
    CancellationToken cancellationToken)
    : IBuilder<GraphBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(GraphBuildContext ctx)
    {
        var setup = ctx.Setup;
        var setupBindingIds = new HashSet<int>(setup.Bindings.Select(i => i.Id));
        var nodes = ctx.Nodes;
        var accumulators = ctx.Accumulators;
        var nodesLength = nodes.Length;
        var map = new Dictionary<Injection, DependencyNode>(nodesLength, injectionComparer);
        var contextMap = new Dictionary<Injection, DependencyNode>(nodesLength, injectionComparer);
        var queue = new Queue<IProcessingNode>(nodesLength >> 2);
        var maxBindingId = 0;
        var processed = new HashSet<IProcessingNode>();
        foreach (var processingNode in nodes)
        {
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
                if (node.Root.Source.Kind.HasFlag(RootKinds.Light)
                    && node.Root.Source.RootType is not INamedTypeSymbol { IsGenericType: true })
                {
                    processed.Add(processingNode);
                }
                else
                {
                    queue.Enqueue(processingNode);
                }
            }
        }

        var processedInjection = new HashSet<Injection>(injectionComparer);
        var notProcessed = new HashSet<IProcessingNode>();
        var edgesMap = new Dictionary<IProcessingNode, List<Dependency>>();
        var injectionPositionComparer = new InjectionPositionComparer(injectionComparer);
        var handledInjections = new Dictionary<IProcessingNode, HashSet<(Injection Injection, int? Position)>>();
        var counter = 0;
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            cancellationToken.ThrowIfCancellationRequested();
            if (counter++ > globalProperties.MaxDependencies)
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
                var hasSourceNode = TryGetSourceNode(map, injection, out var sourceNode);
                var bypassSelfFactoryOverride = hasSourceNode
                                                && sourceNode!.Binding.Id == targetNode.Binding.Id
                                                && injection.Kind == InjectionKind.FactoryInjection
                                                && targetNode.Factory is not null;

                if (bypassSelfFactoryOverride
                    && injection.Type is { IsAbstract: false, TypeKind: not TypeKind.Delegate, SpecialType: Microsoft.CodeAnalysis.SpecialType.None }
                    && !types.IsUnionType(targetNode.Binding.SemanticModel.Compilation, injection.Type))
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
                        queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, newNode));
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
                        ref maxBindingId,
                        accs,
                        hasExplicitDefaultValue,
                        explicitDefaultValue);

                    var accumulatorNodes = dependencyNodePrioritizer.SortByPriority(nodesFactory.CreateNodes(setup, typeConstructor, accumulatorBinding));
                    if (accumulatorNodes.FirstOrDefault() is {} newNode)
                    {
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        var newProcessingNode = CreateNewProcessingNode(newInjection.Tag, newNode);
                        UpdateMap(newInjection, newNode);
                        queue.Enqueue(newProcessingNode);
                    }

                    continue;
                }

                if (types.IsUnionType(targetNode.Binding.SemanticModel.Compilation, injection.Type))
                {
                    AddGenericUnionSources(injection, targetNode);
                    AddFactoryOverrideUnionSources(injection, targetNode);
                }

                if (TryGetSpanConversionSource(setupBindingIds, map, injection, targetNode, out var conversionSource, out var conversionSourceType)
                    || TryGetUnionConversionSource(setup, setupBindingIds, map, injection, targetNode, out conversionSource, out conversionSourceType))
                {
                    var conversionBinding = bindingsFactory.CreateImplicitConversionBinding(
                        setup,
                        targetNode,
                        injection,
                        conversionSource,
                        conversionSourceType,
                        typeConstructor,
                        ++maxBindingId);

                    foreach (var newNode in nodesFactory.CreateNodes(setup, typeConstructor, conversionBinding))
                    {
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        UpdateMap(newInjection, newNode);
                        queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, newNode));
                    }

                    continue;
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
                            var genericNode = genericNodes.Find(i => i.Variation == sourceNode.Variation);
                            if (genericNode is not null)
                            {
                                UpdateMap(newInjection, genericNode);
                                queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, genericNode));
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
                                foreach (var dependencyNode in genericNodes)
                                {
                                    yield return dependencyNode;
                                }

                                isDone = true;
                                break;
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
                                        var processingNode = CreateNewProcessingNode(newInjection.Tag, newNode);
                                        queue.Enqueue(processingNode);
                                    }

                                    continue;
                            }
                        }

                        // OnCannotResolve
                        if (TryCreateOnCannotResolve(setup, typeConstructor, targetNode, injection, ref maxBindingId, map, processed))
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
                        queue.Enqueue(CreateNewProcessingNode(newInjection.Tag, newNode));
                    }

                    continue;
                }

                // Auto-binding
                // Union types are never auto-constructed: an empty union instance has no case value,
                // so an unresolved union contract must surface as a missing binding instead.
                if (injection.Type is { IsAbstract: false, SpecialType: Microsoft.CodeAnalysis.SpecialType.None }
                    && !types.IsUnionType(targetNode.Binding.SemanticModel.Compilation, injection.Type))
                {
                    var disableAutoBinding = false;
                    if (setup.Hints.DisableAutoBinding)
                    {
                        string GetTypeName() => typeResolver.Resolve(setup, injection.Type).Name;
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
                        var autoBinding = bindingsFactory.CreateAutoBinding(setup, targetNode, injection, typeConstructor, ++maxBindingId);
                        var autoNodes = dependencyNodePrioritizer.SortByPriority(nodesFactory.CreateNodes(setup, typeConstructor, autoBinding)).ToList();
                        foreach (var autoNode in autoNodes)
                        {
                            yield return autoNode;
                        }

                        if (injection.Type is INamedTypeSymbol { IsGenericType: true })
                        {
                            yield break;
                        }

                        if (autoNodes.Count > 0)
                        {
                            var newNode = autoNodes[0];
                            var contextTag = GetContextTag(injection, newNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            var newProcessingNode = CreateNewProcessingNode(newInjection.Tag, newNode);
                            UpdateMap(newInjection, newNode);
                            queue.Enqueue(newProcessingNode);
                        }

                        continue;
                    }
                }

                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, typeConstructor, targetNode, injection, ref maxBindingId, map, processed))
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

                var dependency = TryGetSourceNode(map, injection.Injection, out var sourceNode) && sourceNode.Error is null
                    ? new Dependency(true, sourceNode, injection.Injection, node.Node, injection.Position)
                    : new Dependency(false, new DependencyNode(0, node.Node.Binding, node.Node.TypeConstructor), injection.Injection, node.Node, injection.Position, sourceNode?.Error);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges));
        }

        IGraph<DependencyNode, Dependency> graph = new Graph<DependencyNode, Dependency>(entries);
        var lastId = maxBindingId;
        graph = graphOverrider.Rewrite(setup, graph, ref maxBindingId);
        // Has overrides
        if (lastId != maxBindingId)
        {
            graph = graphCleaner.Rewrite(setup, graph, ref maxBindingId);
        }

        ctx.Graph = graph;
        yield break;

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
                injections = new HashSet<(Injection Injection, int? Position)>(injectionPositionComparer);
                handledInjections.Add(processingNode, injections);
            }

            injections.Add((injection, position));
        }

        bool IsInjectionHandled(IProcessingNode processingNode, Injection injection, int? position) =>
            handledInjections.TryGetValue(processingNode, out var injections) && injections.Contains((injection, position));

        bool TryGetSourceNode(IDictionary<Injection, DependencyNode> sourceMap, Injection injection, [NotNullWhen(true)] out DependencyNode? sourceNode)
        {
            if (sourceMap.TryGetValue(injection, out sourceNode))
            {
                return true;
            }

            if (injection.Type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated })
            {
                return sourceMap.TryGetValue(injection with { Type = injection.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) }, out sourceNode);
            }

            sourceNode = null;
            return false;
        }

        void AddGenericUnionSources(Injection unionInjection, DependencyNode targetNode)
        {
            var compilation = targetNode.Binding.SemanticModel.Compilation;
            var caseTypes = types.GetUnionCaseTypes(compilation, unionInjection.Type);
            if (caseTypes.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var candidate in map.ToList())
            {
                if (!setupBindingIds.Contains(candidate.Value.Binding.Id)
                    || candidate.Value.Error is not null
                    || !Injection.EqualTags(unionInjection.Tag, candidate.Key.Tag)
                    || !marker.IsMarkerBased(setup, candidate.Key.Type))
                {
                    continue;
                }

                foreach (var caseType in caseTypes)
                {
                    var candidateTypeConstructor = typeConstructorFactory();
                    if (!candidateTypeConstructor.TryBind(setup, candidate.Key.Type, caseType))
                    {
                        continue;
                    }

                    var sourceType = candidateTypeConstructor.Construct(setup, candidate.Key.Type);
                    if (!types.IsImplicitUnionConversion(compilation, sourceType, unionInjection.Type))
                    {
                        continue;
                    }

                    var sourceInjection = unionInjection with { Type = sourceType };
                    if (TryGetSourceNode(map, sourceInjection, out _))
                    {
                        break;
                    }

                    var genericBinding = bindingsFactory.CreateGenericBinding(
                        setup,
                        sourceInjection,
                        candidate.Value,
                        candidateTypeConstructor,
                        ++maxBindingId);

                    var genericNodes = nodesFactory.CreateNodes(setup, candidateTypeConstructor, genericBinding).ToList();
                    var genericNode = genericNodes.Find(i => i.Variation == candidate.Value.Variation)
                                      ?? genericNodes.FirstOrDefault();
                    if (genericNode is null)
                    {
                        continue;
                    }

                    setupBindingIds.Add(genericBinding.Id);
                    UpdateMap(sourceInjection, genericNode);
                    queue.Enqueue(CreateNewProcessingNode(sourceInjection.Tag, genericNode));
                    break;
                }
            }
        }

        void AddFactoryOverrideUnionSources(Injection unionInjection, DependencyNode targetNode)
        {
            if (targetNode.Factory is not {} factory)
            {
                return;
            }

            var compilation = targetNode.Binding.SemanticModel.Compilation;
            foreach (var resolver in factory.Resolvers)
            {
                var resolverType = targetNode.TypeConstructor.Construct(setup, resolver.Injection.Type);
                if (!types.TypeEquals(resolverType, unionInjection.Type))
                {
                    continue;
                }

                foreach (var @override in resolver.Overrides)
                {
                    foreach (var overrideInjection in @override.Injections)
                    {
                        var sourceInjection = overrideInjection with
                        {
                            Type = targetNode.TypeConstructor.Construct(setup, overrideInjection.Type)
                        };

                        if (!Injection.EqualTags(unionInjection.Tag, sourceInjection.Tag)
                            || !types.IsImplicitUnionConversion(compilation, sourceInjection.Type, unionInjection.Type)
                            || TryGetSourceNode(map, sourceInjection, out _))
                        {
                            continue;
                        }

                        var overrideBinding = bindingsFactory.CreateConstructBinding(
                            setup,
                            targetNode,
                            sourceInjection,
                            sourceInjection.Type,
                            Lifetime.Transient,
                            targetNode.TypeConstructor,
                            ++maxBindingId,
                            MdConstructKind.Override,
                            state: @override);

                        var overrideNode = nodesFactory.CreateNodes(setup, targetNode.TypeConstructor, overrideBinding).FirstOrDefault();
                        if (overrideNode is null)
                        {
                            continue;
                        }

                        setupBindingIds.Add(overrideBinding.Id);
                        UpdateMap(sourceInjection, overrideNode);
                        queue.Enqueue(CreateNewProcessingNode(sourceInjection.Tag, overrideNode));
                    }
                }
            }
        }
    }

    private sealed class InjectionPositionComparer(IInjectionComparer injectionComparer) : IEqualityComparer<(Injection Injection, int? Position)>
    {
        public bool Equals((Injection Injection, int? Position) x, (Injection Injection, int? Position) y) =>
            injectionComparer.Equals(x.Injection, y.Injection)
            && x.Position == y.Position;

        public int GetHashCode((Injection Injection, int? Position) obj)
        {
            unchecked
            {
                var hash = injectionComparer.GetHashCode(obj.Injection);
                hash = hash * 397 ^ (obj.Position ?? 0);
                return hash;
            }
        }
    }

    private MdConstructKind GetConstructKind(INamedTypeSymbol geneticType)
    {
        var unboundGenericType = geneticType.ConstructUnboundGenericType();
        return constructKinds.Get(new ConstructKindKey(unboundGenericType, typeSymbolComparer), key => symbolNames.GetGlobalName(key.TypeSymbol) switch
        {
            Names.SpanTypeName => MdConstructKind.Span,
            Names.ReadOnlySpanTypeName => MdConstructKind.Span,
            Names.IEnumerableTypeName => MdConstructKind.Enumerable,
            Names.IAsyncEnumerableTypeName => MdConstructKind.AsyncEnumerable,
            _ => MdConstructKind.None
        });
    }

    private bool TryGetSpanConversionSource(
        HashSet<int> setupBindingIds,
        IReadOnlyDictionary<Injection, DependencyNode> map,
        Injection injection,
        DependencyNode targetNode,
        [NotNullWhen(true)] out DependencyNode? sourceNode,
        [NotNullWhen(true)] out ITypeSymbol? sourceType)
    {
        sourceNode = null;
        sourceType = null;
        if (!IsSpanType(injection.Type))
        {
            return false;
        }

        var compilation = targetNode.Binding.SemanticModel.Compilation;
        foreach (var candidate in map.OrderByDescending(i => i.Value.Binding.Id))
        {
            if (candidate.Value.Binding.Id == targetNode.Binding.Id
                || !setupBindingIds.Contains(candidate.Value.Binding.Id)
                || candidate.Value.Error is not null
                || !Injection.EqualTags(injection.Tag, candidate.Key.Tag)
                || !IsSpanConversionSource(candidate.Key.Type)
                || !compilation.ClassifyConversion(candidate.Key.Type, injection.Type).IsImplicit)
            {
                continue;
            }

            sourceNode = candidate.Value;
            sourceType = candidate.Key.Type;
            return true;
        }

        return false;
    }

    private bool TryGetUnionConversionSource(
        MdSetup setup,
        HashSet<int> setupBindingIds,
        IReadOnlyDictionary<Injection, DependencyNode> map,
        Injection injection,
        DependencyNode targetNode,
        [NotNullWhen(true)] out DependencyNode? sourceNode,
        [NotNullWhen(true)] out ITypeSymbol? sourceType)
    {
        sourceNode = null;
        sourceType = null;
        var compilation = targetNode.Binding.SemanticModel.Compilation;
        if (!types.IsUnionType(compilation, injection.Type))
        {
            return false;
        }

        var candidates = new Dictionary<ITypeSymbol, DependencyNode>(typeSymbolComparer.Runtime);
        foreach (var candidate in map)
        {
            if (candidate.Value.Binding.Id == targetNode.Binding.Id
                || !setupBindingIds.Contains(candidate.Value.Binding.Id)
                || candidate.Value.Error is not null
                || !Injection.EqualTags(injection.Tag, candidate.Key.Tag)
                || marker.IsMarkerBased(setup, candidate.Key.Type)
                && !marker.IsMarkerBased(setup, injection.Type)
                || !types.IsImplicitUnionConversion(compilation, candidate.Key.Type, injection.Type))
            {
                continue;
            }

            // Several bindings of the same source type follow the usual override rule.
            if (candidates.TryGetValue(candidate.Key.Type, out var currentCandidate)
                && currentCandidate.Binding.Id >= candidate.Value.Binding.Id)
            {
                continue;
            }

            candidates[candidate.Key.Type] = candidate.Value;
        }

        switch (candidates.Count)
        {
            case 0:
                return false;

            case 1:
                var candidate = candidates.First();
                sourceType = candidate.Key;
                sourceNode = candidate.Value;
                return true;

            default:
                var locations = injection.Locations
                    .Concat(candidates.Values.Select(i => locationProvider.GetLocation(i.Binding.Source)))
                    .Distinct()
                    .ToImmutableArray();

                if (locations.IsEmpty)
                {
                    locations = ImmutableArray.Create(locationProvider.GetLocation(setup.Source));
                }

                throw new CompileErrorException(
                    string.Format(
                        Strings.Error_Template_AmbiguousUnionCaseBindings,
                        injection.Type,
                        injection.Tag.ValueToString(),
                        string.Join(", ", candidates.Select(i => $"{i.Key} [{i.Value.Lifetime.ValueToString()}; {i.Value.Binding.Source}]"))),
                    locations,
                    LogId.ErrorAmbiguousUnionCaseBindings,
                    nameof(Strings.Error_Template_AmbiguousUnionCaseBindings));
        }
    }

    private bool IsSpanConversionSource(ITypeSymbol type) =>
        type is IArrayTypeSymbol { Rank: 1 }
        || type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_String
        || IsSpanType(type);

    private bool IsSpanType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return false;
        }

        var typeName = symbolNames.GetGlobalName(namedType.ConstructUnboundGenericType());
        return typeName == Names.ReadOnlySpanTypeName || typeName == Names.SpanTypeName;
    }

    private bool TryCreateOnCannotResolve(MdSetup setup,
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
                    processed.Add(CreateNewProcessingNode(unresolvedInjection.Tag, onCannotResolveNode));
                    return true;
                }
            }
        }

        return false;
    }

    private IProcessingNode CreateNewProcessingNode(object? contextTag, DependencyNode dependencyNode) =>
        processingNodeBuilder.Build(new ProcessingNodeContext(dependencyNode, contextTag));

    private static object? GetContextTag(Injection injection, DependencyNode node) =>
        node.Factory is { Source.HasContextTag: true } ? injection.Tag : null;

    internal readonly struct ConstructKindKey(INamedTypeSymbol typeSymbol, ITypeSymbolComparer typeSymbolComparer) : IEquatable<ConstructKindKey>
    {
        public readonly INamedTypeSymbol TypeSymbol = typeSymbol;

        public bool Equals(ConstructKindKey other) => typeSymbolComparer.RuntimeEquals(TypeSymbol, other.TypeSymbol);

        public override bool Equals(object? obj) => obj is ConstructKindKey other && Equals(other);

        public override int GetHashCode() => typeSymbolComparer.GetRuntimeHashCode(TypeSymbol);
    }
}
