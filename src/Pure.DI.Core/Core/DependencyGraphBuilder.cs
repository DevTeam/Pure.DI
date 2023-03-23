// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

internal class DependencyGraphBuilder : IBuilder<MdSetup, DependencyGraph>
{
    private readonly IBuilder<MdSetup, IEnumerable<DependencyNode>>[] _dependencyNodeBuilders;
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private readonly Func<ITypeConstructor> _typeConstructorFactory;
    private readonly Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> _factoryRewriterFactory;

    public DependencyGraphBuilder(
        IBuilder<MdSetup, IEnumerable<DependencyNode>>[] dependencyNodeBuilders,
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor,
        IBuilder<MdBinding, ISet<Injection>> injectionsBuilder,
        Func<ITypeConstructor> typeConstructorFactory,
        Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory)
    {
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
        _injectionsBuilder = injectionsBuilder;
        _typeConstructorFactory = typeConstructorFactory;
        _factoryRewriterFactory = factoryRewriterFactory;
    }

    public DependencyGraph Build(MdSetup setup, CancellationToken cancellationToken)
    {
        var enumerators = 
            _dependencyNodeBuilders
                .SelectMany(builder => builder.Build(setup, cancellationToken))
                .GroupBy(i => i.Binding)
                .Select(i => Sort(i).Select(i => new ProcessingNode(i, _marker, _injectionsBuilder)).GetEnumerator())
                .ToArray();

        // Initialize enumerators
        foreach (var enumerator in enumerators)
        {
            enumerator.MoveNext();
        }

        DependencyGraph? first = default;
        bool hasNext;
        var isFirts = true;
        do
        {
            hasNext = false;
            cancellationToken.ThrowIfCancellationRequested();
            var nodes = new List<ProcessingNode>();
            foreach (var enumerator in enumerators)
            {
                var current = enumerator.Current;
                if (!hasNext && !isFirts)
                {
                    if (enumerator.MoveNext())
                    {
                        hasNext = true;
                        current = enumerator.Current;
                    }
                }

                nodes.Add(current);
            }

            hasNext |= isFirts;
            if (hasNext)
            {
                var graph = Build(setup, nodes, cancellationToken);
                if (graph.IsValid)
                {
                    return graph;
                }

                first ??= graph;
            }
            
            isFirts = false;
        } while (hasNext);

        return first!;
    }

    private DependencyGraph Build(MdSetup setup, IEnumerable<ProcessingNode> nodes, CancellationToken cancellationToken)
    {
        var queue = new Queue<ProcessingNode>();
        var map = new Dictionary<Injection, DependencyNode>();
        var maxId = 0;
        foreach (var node in nodes)
        {
            var id = node.Node.Binding.Id;
            if (id > maxId)
            {
                maxId = id;
            }
                
            if (node.Node.Root is not { })
            {
                foreach (var exposedInjection in node.ExposedInjections)
                {
                    map[exposedInjection] = node.Node;
                }
            }

            if (!node.IsMarkerBased)
            {
                queue.Enqueue(node);
            }
        }

        var isValid = true;
        var processed = new List<ProcessingNode>();
        var notProcessed = new List<ProcessingNode>();
        while (queue.TryDequeue(out var node))
        {
            var targetNode = node.Node;
            var semanticModel = targetNode.Binding.SemanticModel;
            var compilation = semanticModel.Compilation;
            var isProcessed = true;
            foreach (var injection in node.Injections)
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
                        var unboundType = _unboundTypeConstructor.Construct(compilation, injection.Type);
                        var unboundInjection = injection with { Type = unboundType };
                        if (map.TryGetValue(unboundInjection, out sourceNode))
                        {
                            var newBinding = GreateGenericBinding(targetNode, injection, sourceNode, ++maxId, cancellationToken);
                            var newNode = CreateNode(node, sourceNode.Variation, setup, newBinding, cancellationToken);
                            map.Add(injection, newNode.Node);
                            queue.Enqueue(newNode);
                            continue;
                        }

                        // Construct
                        if (geneticType.TypeArguments is [{ } enumerableType])
                        {
                            var constructKind = geneticType.ConstructUnboundGenericType().ToString() switch
                            {
                                "System.Collections.Immutable.ImmutableArray<>" => MdConstructKind.ImmutableArray,
                                "System.Collections.Generic.IEnumerable<>" => MdConstructKind.Enumerable,
                                _ => default(MdConstructKind?)
                            };

                            if (constructKind.HasValue)
                            {
                                var enumerableBinding = CreateConstructBinding(setup, targetNode, injection, enumerableType, ++maxId, constructKind.Value);
                                var newNode = CreateNode(node, 0, setup, enumerableBinding, cancellationToken);
                                map.Add(injection, newNode.Node);
                                queue.Enqueue(newNode);
                                continue;
                            }
                        }
                        
                        break;
                    }

                    // Array
                    case IArrayTypeSymbol arrayType:
                    {
                        var arrayBinding = CreateConstructBinding(setup, targetNode, injection, arrayType.ElementType, ++maxId, MdConstructKind.Array);
                        var newNode = CreateNode(node, 0, setup, arrayBinding, cancellationToken);
                        map.Add(injection, newNode.Node);
                        queue.Enqueue(newNode);
                        continue;
                    }
                }

                // Auto-binding
                if (injection.Type is { IsAbstract: false, SpecialType: SpecialType.None })
                {
                    var newBinding = CreateAutoBinding(setup, targetNode, injection, ++maxId);
                    var newNode = CreateNode(node, 0, setup, newBinding, cancellationToken);
                    map.Add(injection, newNode.Node);
                    queue.Enqueue(newNode);
                    continue;
                }

                // Not processed
                isProcessed = false;
                break;
            }

            if (isProcessed)
            {
                processed.Add(node);
            }
            else
            {
                notProcessed.Add(node);
                isValid = false;
                break;
            }
        }

        var entriesBuilder = ImmutableArray.CreateBuilder<GraphEntry<DependencyNode, Dependency>>();
        var unresolvedSource = new DependencyNode(0);
        foreach (var node in processed)
        {
            var edges = ImmutableArray.CreateBuilder<Dependency>();
            foreach (var injection in node.Injections)
            {
                var dependency = map.TryGetValue(injection, out var sourceNode)
                    ? new Dependency(true, sourceNode, injection, node.Node)
                    : new Dependency(false, unresolvedSource, injection, node.Node);
                
                edges.Add(dependency);
            }
            
            entriesBuilder.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
        }
        
        // ReSharper disable once InvertIf
        if (notProcessed.Any())
        {
            var edges = ImmutableArray.CreateBuilder<Dependency>();
            foreach (var node in notProcessed)
            {
                foreach (var injection in node.Injections)
                {
                    edges.Add(new Dependency(false, unresolvedSource, injection, node.Node));
                }
                
                entriesBuilder.Add(new GraphEntry<DependencyNode, Dependency>(node.Node, edges.ToImmutableArray()));
            }
        }
        
        return new DependencyGraph(
            isValid,
            setup,
            new Graph<DependencyNode, Dependency>(entriesBuilder.ToImmutable()),
            map.ToImmutableDictionary(),
            ImmutableDictionary<Injection, Root>.Empty);
    }

    private MdBinding GreateGenericBinding(
        DependencyNode targetNode,
        Injection injection,
        DependencyNode sourceNode,
        int newId,
        CancellationToken cancellationToken)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var compilation = semanticModel.Compilation;
        var typeConstructor = _typeConstructorFactory();
        typeConstructor.Bind(sourceNode.Type, injection.Type);
        var newContracts = sourceNode.Binding.Contracts.Select(contract => contract with { ContractType = typeConstructor.Construct(semanticModel.Compilation, contract.ContractType) }).ToImmutableArray();
        var newBinding = sourceNode.Binding with
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
                ? _factoryRewriterFactory().Build(
                    new RewriterContext<MdFactory>(typeConstructor, sourceNode.Binding.Factory.Value),
                    cancellationToken)
                : default(MdFactory?),
            Arg = sourceNode.Binding.Arg.HasValue
                ? sourceNode.Binding.Arg.Value with
                {
                    Type = typeConstructor.Construct(compilation, sourceNode.Binding.Arg.Value.Type)
                }
                : default(MdArg?)
        };
        return newBinding;
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
        var typeConstructor = _typeConstructorFactory();
        if (_marker.IsMarkerBased(injection.Type))
        {
            typeConstructor.Bind(injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(compilation, injection.Type);
        }

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ImmutableArray<MdTag>.Empty));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            semanticModel,
            newContracts,
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(semanticModel, setup.Source, Lifetime.Transient),
            new MdImplementation(semanticModel, setup.Source, sourceType));
        return newBinding;
    }

    private static MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        int newId,
        MdConstructKind constructKind)
    {
        var dependencyContractsBuilder = ImmutableArray.CreateBuilder<MdContract>();
        foreach (var nestedBinding in setup.Bindings.Where(i => i != targetNode.Binding))
        {
            var matchedContracts = nestedBinding.Contracts.Where(i => SymbolEqualityComparer.Default.Equals(i.ContractType, elementType)).ToImmutableArray();
            if (!matchedContracts.Any())
            {
                continue;
            }

            var tag = matchedContracts.First().Tags.Concat(nestedBinding.Tags).Select(i => i.Value).FirstOrDefault();
            var tags = tag is { }
                ? ImmutableArray.Create(new MdTag(targetNode.Binding.SemanticModel, targetNode.Binding.Source, 0, tag))
                : ImmutableArray<MdTag>.Empty;
            dependencyContractsBuilder.Add(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, tags));
        }

        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, ImmutableArray<MdTag>.Empty));
        return new MdBinding(newId, targetNode.Binding.Source, targetNode.Binding.SemanticModel, newContracts, ImmutableArray<MdTag>.Empty)
        {
            Id = newId,
            SemanticModel = targetNode.Binding.SemanticModel,
            Source = targetNode.Binding.Source,
            Construct = new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContractsBuilder.ToImmutable())
        };
    }

    private ProcessingNode CreateNode(
        ProcessingNode targetNode,
        int variation,
        MdSetup setup,
        MdBinding binding,
        CancellationToken cancellationToken)
    {
        var newSetup = setup with
        {
            Roots = ImmutableArray<MdRoot>.Empty,
            Bindings = ImmutableArray.Create(binding)
        };

        var newNode = _dependencyNodeBuilders
            .SelectMany(builder => builder.Build(newSetup, cancellationToken))
            .Single(i => i.Variation == variation);

        return targetNode.CreateNew(newNode);
    }

    private static IEnumerable<DependencyNode> Sort(IEnumerable<DependencyNode> nodes) =>
        nodes
            .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
            .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
            .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility);
}