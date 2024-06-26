// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

internal sealed class DependencyGraphBuilder(
    IEnumerable<IBuilder<MdSetup, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IMarker marker,
    Func<ITypeConstructor> typeConstructorFactory,
    Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
    IFilter filter,
    ICache<INamedTypeSymbol, MdConstructKind> constructKinds,
    IRegistryManager<MdBinding> registryManager,
    CancellationToken cancellationToken)
    : IDependencyGraphBuilder
{
    public IEnumerable<DependencyNode> TryBuild(
        MdSetup setup,
        IReadOnlyCollection<ProcessingNode> nodes,
        out DependencyGraph? dependencyGraph)
    {
        dependencyGraph = default;
        var maxId = 0;
        var map = new Dictionary<Injection, DependencyNode>(nodes.Count);
        var queue = new Queue<ProcessingNode>();
        var roots = new List<DependencyNode>();
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
                
                if (!processingNode.IsMarkerBased)
                {
                    queue.Enqueue(processingNode);
                }
            }
            else
            {
                roots.Add(node);
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
        
        var processed = new HashSet<ProcessingNode>();
        var notProcessed = new HashSet<ProcessingNode>();
        var edgesMap = new Dictionary<ProcessingNode, List<Dependency>>();
        var counter = 0;
        while (queue.TryDequeue(out var node))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (counter++ > Const.MaxIterationsCount)
            {
                throw new CompileErrorException($"The composition is too large. Stopped on the #{counter} dependency.", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }
            
            var targetNode = node.Node;
            var isProcessed = true;
            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue) in node.Injections)
            {
                if (map.TryGetValue(injection, out var sourceNode))
                {
                    if (!marker.IsMarkerBased(sourceNode.Type))
                    {
                        registryManager.Register(setup, sourceNode.Binding);
                        continue;
                    }
                }

                if (accumulators.TryGetValue(injection.Type, out var accs))
                {
                    var accumulatorBinding = CreateAccumulatorBinding(
                        setup,
                        targetNode,
                        ref maxId,
                        accs,
                        hasExplicitDefaultValue,
                        explicitDefaultValue);

                    return CreateNodes(setup, accumulatorBinding);
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol { IsGenericType: true } geneticType:
                    {
                        // Generic
                        var isGenericOk = false;
                        foreach (var item in map)
                        {
                            if (item.Key.Type is not INamedTypeSymbol { IsGenericType: true })
                            {
                                continue;
                            }
                            
                            if (!Injection.EqualTags(injection.Tag, item.Key.Tag))
                            {
                                continue;
                            }
                            
                            var typeConstructor = typeConstructorFactory();
                            if (!typeConstructor.TryBind(item.Key.Type, injection.Type))
                            {
                                continue;
                            }

                            sourceNode = item.Value;
                            registryManager.Register(setup, sourceNode.Binding);
                            var genericBinding = CreateGenericBinding(
                                targetNode,
                                injection,
                                sourceNode,
                                typeConstructor,
                                ++maxId);
                            
                            var genericNode = CreateNodes(setup, genericBinding).Single(i => i.Variation == sourceNode.Variation);
                            map[injection] = genericNode;
                            foreach (var contract in genericBinding.Contracts.Where(i => i.ContractType is not null))
                            {
                                foreach (var tag in contract.Tags.Select(i => i.Value).DefaultIfEmpty(default))
                                {
                                    var newInjection = new Injection(contract.ContractType!, tag);
                                    if (!map.ContainsKey(newInjection))
                                    {
                                        map[newInjection] = genericNode;
                                    }
                                }
                            }

                            queue.Enqueue(CreateNewProcessingNode(setup, injection, genericNode));
                            isGenericOk = true;
                            break;
                        }

                        if (isGenericOk)
                        {
                            continue;
                        }

                        // Construct
                        if (geneticType.TypeArguments is [{ } constructType])
                        {
                            var constructKind = GetConstructKind(geneticType);
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
                                    
                                    foreach (var newNode in CreateNodes(setup, constructBinding))
                                    {
                                        map[injection] = newNode;
                                        var processingNode = CreateNewProcessingNode(setup, injection, newNode);
                                        queue.Enqueue(processingNode);
                                    }
                                    
                                    continue;
                            }
                        }
                        
                        // OnCannotResolve
                        if (TryCreateOnCannotResolve(setup, targetNode, injection, ref maxId, map, processed))
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

                        foreach (var newNode in CreateNodes(setup, arrayBinding))
                        {
                            map[injection] = newNode;
                            queue.Enqueue(CreateNewProcessingNode(setup, injection, newNode));
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
                        default,
                        hasExplicitDefaultValue, explicitDefaultValue);

                    var newSourceNodes = CreateNodes(setup, explicitDefaultBinding);
                    foreach (var newSourceNode in newSourceNodes)
                    {
                        if (!edgesMap.TryGetValue(node, out var edges))
                        {
                            edges = [];
                            edgesMap.Add(node, edges);
                        }

                        edges.Add(new Dependency(true, newSourceNode, injection, targetNode));
                    }
                    
                    continue;
                }

                if (injection.Type.ToString() == setup.Name.FullName)
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
                    
                    foreach (var newNode in CreateNodes(setup, compositionBinding))
                    {
                        map[injection] = newNode;
                        queue.Enqueue(CreateNewProcessingNode(setup, injection, newNode));
                    }

                    continue;
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: SpecialType.None })
                {
                    var autoBinding = CreateAutoBinding(setup, targetNode, injection, ++maxId);
                    return CreateNodes(setup, autoBinding).ToArray();
                }
                
                // OnCannotResolve
                if (TryCreateOnCannotResolve(setup, targetNode, injection, ref maxId, map, processed))
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

        foreach (var key in map.Keys.Where(i => ReferenceEquals(i.Tag, MdTag.ContextTag)).ToArray())
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

            entries.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
        }
        
        dependencyGraph = CreateGraph(setup, roots, entries, map);
        return ImmutableArray<DependencyNode>.Empty;
    }

    private MdConstructKind GetConstructKind(INamedTypeSymbol geneticType)
    {
        var unboundGenericType = geneticType.ConstructUnboundGenericType();
        return constructKinds.Get(unboundGenericType, type => type.ToString() switch
        {
            "System.Span<>" => MdConstructKind.Span,
            "System.ReadOnlySpan<>" => MdConstructKind.Span,
            "System.Collections.Generic.IEnumerable<>" => MdConstructKind.Enumerable,
            "System.Collections.Generic.IAsyncEnumerable<>" => MdConstructKind.AsyncEnumerable,
            _ => MdConstructKind.None
        });
    }

    private static DependencyGraph CreateGraph(
        MdSetup setup,
        IEnumerable<DependencyNode> roots,
        IEnumerable<GraphEntry<DependencyNode, Dependency>> entries,
        IReadOnlyDictionary<Injection, DependencyNode> map)
    {
        var graph = new Graph<DependencyNode, Dependency>(entries.ToImmutableArray());
        var nodes = new HashSet<DependencyNode>();
        foreach (var root in roots)
        {
            if (!nodes.Add(root))
            {
                continue;
            }

            var stack = new Stack<DependencyNode>();
            stack.Push(root);
            while (stack.TryPop(out var node))
            {
                if (graph.TryGetInEdges(node, out var dependencies))
                {
                    foreach (var dependency in dependencies)
                    {
                        var source = dependency.Source;
                        if (nodes.Add(source))
                        {
                            stack.Push(source);
                        }
                    }
                }
            }
        }

        var consolidatedGraph = graph.Consolidate(nodes);
        var isResolved = consolidatedGraph.Edges.All(i => i.IsResolved);
        return new DependencyGraph(
            isResolved,
            setup,
            consolidatedGraph,
            map,
            ImmutableSortedDictionary<Injection, Root>.Empty
        );
    }

    private bool TryCreateOnCannotResolve(
        MdSetup setup,
        DependencyNode ownerNode,
        Injection unresolvedInjection,
        ref int maxId,
        IDictionary<Injection, DependencyNode> map,
        ISet<ProcessingNode> processed)
    {
        if (setup.Hints.IsOnCannotResolveEnabled
            && filter.IsMeetRegularExpression(
                setup,
                (Hint.OnCannotResolveContractTypeNameRegularExpression, unresolvedInjection.Type.ToString()),
                (Hint.OnCannotResolveTagRegularExpression, unresolvedInjection.Tag.ValueToString()),
                (Hint.OnCannotResolveLifetimeRegularExpression, ownerNode.Lifetime.ValueToString())))
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

            var onCannotResolveNodes = CreateNodes(setup, onCannotResolveBinding);
            foreach (var onCannotResolveNode in onCannotResolveNodes)
            {
                map[unresolvedInjection] = onCannotResolveNode;
                processed.Add(CreateNewProcessingNode(setup, unresolvedInjection, onCannotResolveNode));
                return true;
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
                ContractType = typeConstructor.Construct(compilation, contract.ContractType!),
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
                ? factoryRewriterFactory().Build(
                    new RewriterContext<MdFactory>(typeConstructor, injection, sourceNode.Binding.Factory.Value))
                : default(MdFactory?),
            Arg = sourceNode.Binding.Arg.HasValue
                ? sourceNode.Binding.Arg.Value with
                {
                    Type = typeConstructor.Construct(compilation, sourceNode.Binding.Arg.Value.Type)
                }
                : default(MdArg?)
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
            default,
            default,
            default,
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
        if (marker.IsMarkerBased(injection.Type))
        {
            var typeConstructor = typeConstructorFactory();
            typeConstructor.TryBind(injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(compilation, injection.Type);
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
            new MdImplementation(semanticModel, setup.Source, sourceType));
        return newBinding;
    }

    private MdBinding CreateConstructBinding(MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        int newId,
        MdConstructKind constructKind,
        object? tag = default,
        bool hasExplicitDefaultValue = default,
        object? explicitDefaultValue = default)
    {
        elementType = elementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var dependencyContracts = new List<MdContract>();
        var contracts = new HashSet<Injection>();
        foreach (var nestedBinding in setup.Bindings.Where(i => i != targetNode.Binding))
        {
            var matchedContracts = GetMatchedMdContracts(targetNode.Binding.SemanticModel.Compilation, elementType, nestedBinding).ToArray();
            if (matchedContracts.Length == 0)
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
                foreach (var mdTag in tags.DefaultIfEmpty(new MdTag(0, default)))
                {
                    if (!contracts.Add(new Injection(elementType, mdTag)))
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
            default,
            default,
            default,
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

    private IEnumerable<MdContract> GetMatchedMdContracts(Compilation compilation, ITypeSymbol elementType, MdBinding nestedBinding)
    {
        foreach (var contract in nestedBinding.Contracts)
        {
            var contractType = contract.ContractType;
            if (contractType is not null && marker.IsMarkerBased(contractType))
            {
                var typeConstructor = typeConstructorFactory();
                if (typeConstructor.TryBind(contractType, elementType))
                {
                    contractType = typeConstructor.Construct(compilation, contractType);
                }
            }
            
            if (SymbolEqualityComparer.Default.Equals(contractType, elementType))
            {
                yield return contract;
            }
        }
    }

    private ProcessingNode CreateNewProcessingNode(MdSetup setup, in Injection injection, DependencyNode dependencyNode)
    {
        registryManager.Register(setup, dependencyNode.Binding);
        var contracts = contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, injection.Tag));
        return new ProcessingNode(dependencyNode, contracts, marker);
    }

    private IEnumerable<DependencyNode> CreateNodes(MdSetup setup, MdBinding binding)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        return dependencyNodeBuilders.SelectMany(builder => builder.Build(newSetup));
    }
}