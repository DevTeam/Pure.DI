// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable IdentifierTypo

namespace Pure.DI.Core;

using Injection=Injection;

sealed class DependencyGraphBuilder(
    IEnumerable<IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IMarker marker,
    Func<ITypeConstructor> typeConstructorFactory,
    Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
    IFilter filter,
    ICache<INamedTypeSymbol, MdConstructKind> constructKinds,
    IRegistryManager<MdBinding> registryManager,
    ISymbolNames symbolNames,
    ITypes types,
    Func<DependencyNode, ISet<Injection>, IProcessingNode> processingNodeFactory,
    CancellationToken cancellationToken)
    : IDependencyGraphBuilder
{
    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<IProcessingNode> nodes,
        out DependencyGraph? dependencyGraph)
    {
        dependencyGraph = null;
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
                    setup.Source.GetLocation(),
                    LogId.ErrorInvalidMetadata);
            }

            var targetNode = node.Node;
            var isProcessed = true;
            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue) in node.Injections)
            {
                if (!processedInjection.Add(injection))
                {
                    continue;
                }

                if (map.TryGetValue(injection, out var sourceNode))
                {
                    registryManager.Register(setup, sourceNode.Binding);
                    if (!marker.IsMarkerBased(setup, sourceNode.Type))
                    {
                        queue.Enqueue(CreateNewProcessingNode(setup, injection.Tag, sourceNode));
                        continue;
                    }
                }

                var typeConstructor = typeConstructorFactory();
                if (accumulators.TryGetValue(injection.Type, out var accs))
                {
                    var accumulatorBinding = CreateAccumulatorBinding(
                        setup,
                        targetNode,
                        ref maxId,
                        accs,
                        hasExplicitDefaultValue,
                        explicitDefaultValue);

                    return CreateNodes(setup, typeConstructor, accumulatorBinding);
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol namedTypeSymbol
                        when namedTypeSymbol.IsGenericType || marker.IsMarkerBased(setup, namedTypeSymbol):
                    {
                        var isDone = false;
                        foreach (var item in map)
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
                            registryManager.Register(setup, sourceNode.Binding);
                            var contextTag = GetContextTag(injection, sourceNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            var genericBinding = CreateGenericBinding(
                                setup,
                                targetNode,
                                newInjection,
                                sourceNode,
                                typeConstructor,
                                ++maxId);

                            var genericNode = CreateNodes(setup, typeConstructor, genericBinding).Single(i => i.Variation == sourceNode.Variation);
                            UpdateMap(newInjection, genericNode);
                            queue.Enqueue(CreateNewProcessingNode(setup, newInjection.Tag, genericNode));
                            foreach (var contract in genericBinding.Contracts.Where(i => i.ContractType is not null))
                            {
                                foreach (var tag in contract.Tags.Select(i => i.Value).DefaultIfEmpty(null))
                                {
                                    newInjection = new Injection(InjectionKind.Contract, contract.ContractType!, contextTag ?? tag);
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
                                    var constructBinding = CreateConstructBinding(
                                        setup,
                                        targetNode,
                                        injection,
                                        constructType,
                                        lifetime,
                                        ++maxId,
                                        constructKind);

                                    foreach (var newNode in CreateNodes(setup, typeConstructor, constructBinding))
                                    {
                                        var contextTag = GetContextTag(injection, newNode);
                                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                                        UpdateMap(newInjection, newNode);
                                        var processingNode = CreateNewProcessingNode(setup, newInjection.Tag, newNode);
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
                        var arrayBinding = CreateConstructBinding(
                            setup,
                            targetNode,
                            injection,
                            arrayType.ElementType,
                            Lifetime.Transient,
                            ++maxId,
                            MdConstructKind.Array);

                        foreach (var newNode in CreateNodes(setup, typeConstructor, arrayBinding))
                        {
                            var contextTag = GetContextTag(injection, newNode);
                            var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                            UpdateMap(newInjection, newNode);
                            queue.Enqueue(CreateNewProcessingNode(setup, newInjection.Tag, newNode));
                        }

                        continue;
                    }
                }

                // ExplicitDefaultValue
                if (hasExplicitDefaultValue)
                {
                    var explicitDefaultBinding = CreateConstructBinding(
                        setup,
                        targetNode,
                        injection,
                        injection.Type,
                        Lifetime.Transient,
                        ++maxId,
                        MdConstructKind.ExplicitDefaultValue,
                        null,
                        hasExplicitDefaultValue, explicitDefaultValue);

                    var newSourceNodes = CreateNodes(setup, typeConstructor, explicitDefaultBinding);
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
                    var compositionBinding = CreateConstructBinding(
                        setup,
                        targetNode,
                        injection,
                        injection.Type,
                        Lifetime.Transient,
                        ++maxId,
                        MdConstructKind.Composition);

                    foreach (var newNode in CreateNodes(setup, typeConstructor, compositionBinding))
                    {
                        var contextTag = GetContextTag(injection, newNode);
                        var newInjection = injection with { Tag = contextTag ?? injection.Tag };
                        UpdateMap(newInjection, newNode);
                        queue.Enqueue(CreateNewProcessingNode(setup, newInjection.Tag, newNode));
                    }

                    continue;
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: Microsoft.CodeAnalysis.SpecialType.None })
                {
                    var autoBinding = CreateAutoBinding(setup, targetNode, injection, ++maxId);
                    return CreateNodes(setup, typeConstructor, autoBinding).ToList();
                }

                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, typeConstructor, targetNode, injection, ref maxId, map, processed))
                {
                    continue;
                }

                // Not processed
                notProcessed.Add(node);
                isProcessed = false;
                break;
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

            foreach (var injectionInfo in node.Injections)
            {
                var injection = injectionInfo.Injection;
                var dependency = map.TryGetValue(injection, out var sourceNode)
                    ? new Dependency(true, sourceNode, injection, node.Node)
                    : new Dependency(false, new DependencyNode(0, node.Node.Binding), injection, node.Node);

                edges.Add(dependency);
            }

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges));
        }

        var graph = new Graph<DependencyNode, Dependency>(entries);
        var isResolved = graph.Edges.All(i => i.IsResolved);
        dependencyGraph = new DependencyGraph(
            isResolved,
            setup,
            graph,
            map,
            ImmutableSortedDictionary<Injection, Root>.Empty
        );

        return ImmutableArray<DependencyNode>.Empty;

        object? GetContextTag(Injection injection, DependencyNode node) =>
            node.Factory is { Source.HasContextTag: true } ? injection.Tag : null;

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
                var onCannotResolveBinding = CreateConstructBinding(
                    setup,
                    ownerNode,
                    unresolvedInjection,
                    unresolvedInjection.Type,
                    Lifetime.Transient,
                    ++maxId,
                    MdConstructKind.OnCannotResolve,
                    unresolvedInjection.Tag);

                var onCannotResolveNodes = CreateNodes(setup, typeConstructor, onCannotResolveBinding);
                foreach (var onCannotResolveNode in onCannotResolveNodes)
                {
                    map[unresolvedInjection] = onCannotResolveNode;
                    processed.Add(CreateNewProcessingNode(setup, unresolvedInjection.Tag, onCannotResolveNode));
                    return true;
                }
            }
        }

        return false;
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
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int newId)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var compilation = semanticModel.Compilation;
        var newContracts = sourceNode.Binding.Contracts
            .Where(contract => contract.ContractType is not null)
            .Select(contract => contract with
            {
                ContractType = typeConstructor.Construct(setup, compilation, contract.ContractType!),
                Tags = contract.Tags.Select(tag => CreateTag(injection, tag)).Where(tag => tag.HasValue).Select(tag => tag!.Value).ToImmutableArray()
            })
            .ToImmutableArray();

        return sourceNode.Binding with
        {
            Id = newId,
            TypeConstructor = typeConstructor,
            Contracts = newContracts,
            Implementation = sourceNode.Binding.Implementation.HasValue
                ? sourceNode.Binding.Implementation.Value with
                {
                    Type = typeConstructor.Construct(setup, compilation, sourceNode.Binding.Implementation.Value.Type)
                }
                : null,
            Factory = sourceNode.Binding.Factory.HasValue
                ? factoryRewriterFactory().Build(
                    new RewriterContext<MdFactory>(setup, typeConstructor, injection, sourceNode.Binding.Factory.Value))
                : null,
            Arg = sourceNode.Binding.Arg.HasValue
                ? sourceNode.Binding.Arg.Value with
                {
                    Type = typeConstructor.Construct(setup, compilation, sourceNode.Binding.Arg.Value.Type)
                }
                : null
        };
    }

    private static MdBinding CreateAccumulatorBinding(MdSetup setup,
        DependencyNode targetNode,
        ref int maxId,
        IReadOnlyCollection<MdAccumulator> accumulators,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue)
    {
        var accumulator = accumulators.First();
        return new MdBinding(
            ++maxId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, accumulator.Source, accumulator.AccumulatorType, ContractKind.Implicit, ImmutableArray<MdTag>.Empty)),
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, accumulator.Source, Lifetime.Transient),
            null,
            null,
            null,
            new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                accumulator.AccumulatorType,
                accumulator.Type,
                MdConstructKind.Accumulator,
                ImmutableArray<MdContract>.Empty,
                hasExplicitDefaultValue,
                explicitDefaultValue,
                accumulators));
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
        var typeConstructor = typeConstructorFactory();
        if (marker.IsMarkerBased(setup, injection.Type))
        {
            typeConstructor.TryBind(setup, injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(setup, compilation, injection.Type);
        }

        var newTags = injection.Tag is not null
            ? ImmutableArray.Create(new MdTag(0, injection.Tag))
            : ImmutableArray<MdTag>.Empty;

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ContractKind.Implicit, ImmutableArray<MdTag>.Empty));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            semanticModel,
            newContracts,
            newTags,
            new MdLifetime(semanticModel, setup.Source, Lifetime.Transient),
            new MdImplementation(semanticModel, setup.Source, sourceType),
            TypeConstructor: typeConstructor);
        return newBinding;
    }

    private MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        int newId,
        MdConstructKind constructKind,
        object? tag = null,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null)
    {
        elementType = elementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var dependencyContracts = new List<MdContract>();
        var contracts = new HashSet<Injection>();
        foreach (var nestedBinding in setup.Bindings.Where(i => i != targetNode.Binding))
        {
            var matchedContracts = GetMatchedMdContracts(setup, targetNode.Binding.SemanticModel.Compilation, elementType, nestedBinding).ToList();
            if (matchedContracts.Count == 0)
            {
                continue;
            }

            var tags = matchedContracts.First().Tags
                .Concat(nestedBinding.Tags)
                .Select((i, position) => i with { Position = position })
                .ToImmutableArray();

            var isDuplicate = false;
            if (constructKind is MdConstructKind.Enumerable or MdConstructKind.Array or MdConstructKind.Span or MdConstructKind.AsyncEnumerable)
            {
                foreach (var mdTag in tags.DefaultIfEmpty(new MdTag(0, null)))
                {
                    if (!contracts.Add(new Injection(InjectionKind.Construct, elementType, mdTag)))
                    {
                        isDuplicate = true;
                    }
                }
            }

            if (isDuplicate)
            {
                continue;
            }

            dependencyContracts.Add(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, ContractKind.Implicit, tags));
        }

        var newTags = tag is not null
            ? ImmutableArray.Create(new MdTag(0, tag))
            : ImmutableArray<MdTag>.Empty;
        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, ContractKind.Implicit, newTags));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            newContracts,
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, targetNode.Binding.Source, lifetime),
            null,
            null,
            null,
            new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContracts.ToImmutableArray(),
                hasExplicitDefaultValue,
                explicitDefaultValue));

        registryManager.Register(setup, newBinding);
        return newBinding;
    }

    private IEnumerable<MdContract> GetMatchedMdContracts(MdSetup setup, Compilation compilation, ITypeSymbol elementType, MdBinding nestedBinding)
    {
        foreach (var contract in nestedBinding.Contracts)
        {
            var contractType = contract.ContractType;
            if (contractType is not null && marker.IsMarkerBased(setup, contractType))
            {
                var typeConstructor = typeConstructorFactory();
                if (typeConstructor.TryBind(setup, contractType, elementType))
                {
                    contractType = typeConstructor.Construct(setup, compilation, contractType);
                }
            }

            if (types.TypeEquals(contractType, elementType))
            {
                yield return contract;
            }
        }
    }

    private IProcessingNode CreateNewProcessingNode(MdSetup setup, object? contextTag, DependencyNode dependencyNode)
    {
        registryManager.Register(setup, dependencyNode.Binding);
        var contracts = contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, contextTag));
        return processingNodeFactory(dependencyNode, contracts);
    }

    private IEnumerable<DependencyNode> CreateNodes(MdSetup setup, ITypeConstructor typeConstructor, MdBinding binding)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        var ctx = new DependencyNodeBuildContext(newSetup, typeConstructor);
        return dependencyNodeBuilders.SelectMany(builder => builder.Build(ctx));
    }
}