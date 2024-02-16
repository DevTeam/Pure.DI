// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

internal sealed class DependencyGraphBuilder(
    IEnumerable<IBuilder<MdSetup, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IMarker marker,
    Func<ITypeConstructor> typeConstructorFactory,
    Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
    IFilter filter,
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
                roots.Add(node);
            }

            if (!processingNode.IsMarkerBased)
            {
                queue.Enqueue(processingNode);
            }
        }

        var processed = new HashSet<ProcessingNode>();
        var notProcessed = new HashSet<ProcessingNode>();
        var edgesMap = new Dictionary<ProcessingNode, List<Dependency>>();
        while (queue.TryDequeue(out var node))
        {
            var targetNode = node.Node;
            var isProcessed = true;
            foreach (var (injection, hasExplicitDefaultValue, explicitDefaultValue) in node.Injections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (map.TryGetValue(injection, out var sourceNode))
                {
                    continue;
                }

                switch (injection.Type)
                {
                    case INamedTypeSymbol { IsGenericType: true } geneticType:
                    {
                        // Generic
                        var isGenericOk = false;
                        foreach (var item in map)
                        {
                            if (!Injection.EqualTags(injection.Tag, item.Key.Tag))
                            {
                                continue;
                            }

                            if (item.Key.Type is not INamedTypeSymbol { IsGenericType: true })
                            {
                                continue;
                            }
                            
                            var typeConstructor = typeConstructorFactory();
                            if (!typeConstructor.TryBind(item.Key.Type, injection.Type))
                            {
                                continue;
                            }

                            sourceNode = item.Value;
                            var genericBinding = CreateGenericBinding(targetNode, injection, sourceNode, typeConstructor, ++maxId);
                            var genericNode = CreateNodes(setup, genericBinding).Single(i => i.Variation == sourceNode.Variation);
                            map[injection] = genericNode;
                            queue.Enqueue(CreateNewProcessingNode(injection, genericNode));
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
                            var constructKind = geneticType.ConstructUnboundGenericType().ToString() switch
                            {
                                "System.Span<>" => MdConstructKind.Span,
                                "System.ReadOnlySpan<>" => MdConstructKind.Span,
                                "System.Collections.Generic.IEnumerable<>" => MdConstructKind.Enumerable,
                                "System.Collections.Generic.IAsyncEnumerable<>" => MdConstructKind.AsyncEnumerable,
                                _ => default(MdConstructKind?)
                            };

                            var lifetime = constructKind == MdConstructKind.Enumerable ? Lifetime.PerBlock : Lifetime.Transient;
                            if (constructKind.HasValue)
                            {
                                var enumerableBinding = CreateConstructBinding(setup, targetNode, injection, constructType, default, lifetime, ++maxId, constructKind.Value, false, default);
                                return CreateNodes(setup, enumerableBinding);
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
                        var arrayBinding = CreateConstructBinding(setup, targetNode, injection, arrayType.ElementType, default, Lifetime.Transient, ++maxId, MdConstructKind.Array, false, default);
                        return CreateNodes(setup, arrayBinding);
                    }
                }
                
                // ExplicitDefaultValue
                if (hasExplicitDefaultValue)
                {
                    var explicitDefaultBinding = CreateConstructBinding(setup, targetNode, injection, injection.Type, default, Lifetime.Transient, ++maxId, MdConstructKind.ExplicitDefaultValue, hasExplicitDefaultValue, explicitDefaultValue);
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
                    var compositionBinding = CreateConstructBinding(setup, targetNode, injection, injection.Type, default, Lifetime.Transient, ++maxId, MdConstructKind.Composition, false, default);
                    return CreateNodes(setup, compositionBinding);
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
        MdSetup mdSetup,
        DependencyNode ownerNode,
        Injection unresolvedInjection,
        ref int maxId,
        IDictionary<Injection, DependencyNode> map,
        ISet<ProcessingNode> processed)
    {
        if (mdSetup.Hints.GetHint<SettingState>(Hint.OnCannotResolve) == SettingState.On
            && filter.IsMeetRegularExpression(
                mdSetup,
                (Hint.OnCannotResolveContractTypeNameRegularExpression, unresolvedInjection.Type.ToString()),
                (Hint.OnCannotResolveTagRegularExpression, unresolvedInjection.Tag.ValueToString()),
                (Hint.OnCannotResolveLifetimeRegularExpression, ownerNode.Lifetime.ValueToString())))
        {
            var onCannotResolveBinding = CreateConstructBinding(
                mdSetup,
                ownerNode,
                unresolvedInjection,
                unresolvedInjection.Type,
                unresolvedInjection.Tag,
                Lifetime.Transient,
                ++maxId,
                MdConstructKind.OnCannotResolve,
                false,
                default);

            var onCannotResolveNodes = CreateNodes(mdSetup, onCannotResolveBinding);
            foreach (var onCannotResolveNode in onCannotResolveNodes)
            {
                map[unresolvedInjection] = onCannotResolveNode;
                processed.Add(CreateNewProcessingNode(unresolvedInjection, onCannotResolveNode));
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
            .Select(contract => contract with
            {
                ContractType = injection.Type,
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

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ImmutableArray<MdTag>.Empty));
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

    private MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        object? tag,
        Lifetime lifetime,
        int newId,
        MdConstructKind constructKind,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue)
    {
        elementType = elementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var dependencyContracts = new List<MdContract>();
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
            dependencyContracts.Add(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, tags));
        }

        var newTags = tag is not null
            ? ImmutableArray.Create(new MdTag(0, tag))
            : ImmutableArray<MdTag>.Empty;
        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, newTags));
        return new MdBinding(
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
    }

    private IEnumerable<MdContract> GetMatchedMdContracts(Compilation compilation, ITypeSymbol elementType, MdBinding nestedBinding)
    {
        foreach (var contract in nestedBinding.Contracts)
        {
            var contractType = contract.ContractType;
            if (marker.IsMarkerBased(contract.ContractType))
            {
                var typeConstructor = typeConstructorFactory();
                if (typeConstructor.TryBind(contract.ContractType, elementType))
                {
                    contractType = typeConstructor.Construct(compilation, contract.ContractType);
                }
            }
            
            if (SymbolEqualityComparer.Default.Equals(contractType, elementType))
            {
                yield return contract;
            }
        }
    }

    private ProcessingNode CreateNewProcessingNode(in Injection injection, DependencyNode dependencyNode)
    {
        var contracts = contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, injection.Tag));
        return new ProcessingNode(dependencyNode, contracts, marker);
    }

    private IEnumerable<DependencyNode> CreateNodes(MdSetup setup, MdBinding binding)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        return dependencyNodeBuilders.SelectMany(builder => builder.Build(newSetup));
    }
}