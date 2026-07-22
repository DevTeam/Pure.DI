namespace Pure.DI.Core.Code;

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.
class Accumulators(
    INodeTools nodeTools,
    IBuildTools buildTools,
    ISymbolNames symbolNames,
    ITypeSymbolComparer typeSymbolComparer)
    : IAccumulators
{
    public IEnumerable<(MdAccumulator, Dependency)> GetAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode)
    {
        var processed = new HashSet<IDependencyNode>();
        var nodes = new Stack<IDependencyNode>();
        nodes.Push(targetNode);
        while (nodes.TryPop(out var node))
        {
            if (!processed.Add(node))
            {
                continue;
            }

            if (!graph.Graph.TryGetInEdges(node.Node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (nodeTools.IsLazy(source.Node, graph))
                {
                    continue;
                }

                if (source.Construct is { Source: { Kind: MdConstructKind.Accumulator, State: IEnumerable<MdAccumulator> mdAccumulators } })
                {
                    foreach (var acc in mdAccumulators)
                    {
                        yield return (acc, dependency);
                    }

                    continue;
                }

                nodes.Push(source);
            }
        }
    }

    public IEnumerable<(MdAccumulator, Dependency)> GetBoundaryAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode)
    {
        if (!graph.Graph.TryGetInEdges(targetNode.Node, out var dependencies))
        {
            yield break;
        }

        foreach (var dependency in dependencies)
        {
            var ownerContract = dependency.Injection.Type;
            // An ownership wrapper both implements its owner contract and receives
            // an implementation of that contract backed by an accumulator.
            if (!IsAssignableTo(targetNode.Node.Type, ownerContract))
            {
                continue;
            }

            foreach (var item in GetBranchAccumulators(graph, dependency))
            {
                if (!typeSymbolComparer.RuntimeEquals(targetNode.Node.Type, item.Item1.AccumulatorType)
                    && IsAssignableTo(item.Item1.AccumulatorType, ownerContract))
                {
                    yield return item;
                }
            }
        }
    }

    public IEnumerable<ITypeSymbol> GetNestedBoundaryAccumulatorTypes(
        DependencyGraph graph,
        IDependencyNode targetNode)
    {
        var processed = new HashSet<IDependencyNode>();
        var nodes = new Stack<IDependencyNode>();
        nodes.Push(targetNode);
        while (nodes.TryPop(out var node))
        {
            if (!processed.Add(node))
            {
                continue;
            }

            foreach (var item in GetBoundaryAccumulators(graph, node))
            {
                yield return item.Item1.AccumulatorType;
            }

            if (!graph.Graph.TryGetInEdges(node.Node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                nodes.Push(dependency.Source);
            }
        }
    }

    public IEnumerable<Accumulator> CreateAccumulators(
        DependencyGraph graph,
        IDependencyNode targetNode,
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap)
    {
        var items = accumulators.ToImmutableArray();
        var builtInStats = new Dictionary<ITypeSymbol, (bool IsEmpty, int Capacity)>(typeSymbolComparer.Runtime);
        foreach (var accumulatorGroup in items
                     .Where(i => IsBuiltInOwnershipAccumulator(i.accumulator.AccumulatorType))
                     .GroupBy(i => i.accumulator.AccumulatorType, typeSymbolComparer.Runtime))
        {
            var accumulatorMetadata = accumulatorGroup
                .Select(i => i.accumulator)
                .ToImmutableArray();
            builtInStats.Add(
                accumulatorGroup.Key,
                (
                    !accumulatorMetadata.Any(i => HasAccumulatedResources(graph, targetNode, i)),
                    GetEagerAccumulatedResourceCount(graph, targetNode, accumulatorMetadata)));
        }

        foreach (var item in items)
        {
            var stats = builtInStats.TryGetValue(item.accumulator.AccumulatorType, out var builtInStat)
                ? builtInStat
                : (IsEmpty: false, Capacity: 0);
            yield return new Accumulator(
                varsMap.GetInjection(graph, item.dependency.Injection, item.dependency.Source),
                item.accumulator.Type,
                item.accumulator.Lifetime,
                stats.IsEmpty,
                stats.Capacity);
        }
    }

    public void BuildAccumulators(CodeContext ctx, bool includeDeclared = false)
    {
        var accumulatorGroups = ctx.Accumulators
            .GroupBy(accumulator => accumulator.VarInjection.Var.Name)
            .Where(group => includeDeclared || !group.First().VarInjection.Var.Declaration.IsDeclared);
        foreach (var accumulatorGroup in accumulatorGroups)
        {
            var accumulator = accumulatorGroup.First();
            var accVar = accumulator.VarInjection.Var;
            var useEmpty = accumulatorGroup.All(i => i.IsEmpty);
            var isOptimizedOwnershipAccumulator = IsBuiltInOwnershipAccumulator(accVar.InstanceType);
            var value = $"new {accVar.InstanceType}()";
            if (useEmpty)
            {
                value = $"{accVar.InstanceType}.Empty";
            }
            else if (isOptimizedOwnershipAccumulator)
            {
                var capacity = accumulatorGroup.Max(i => i.Capacity);
                if (ctx.RootContext.IsThreadSafeEnabled)
                {
                    if (!ctx.RootContext.Root.IsStatic)
                    {
                        ctx.RootContext.LockIsInUse = true;
                    }

                    var lockName = ctx.RootContext.Root.IsStatic ? Names.PerResolveLockFieldName : Names.LockFieldName;
                    value = $"new {accVar.InstanceType}({capacity}, {lockName})";
                }
                else
                {
                    value = $"new {accVar.InstanceType}({capacity})";
                }
            }

            ctx.Lines.AppendLine($"{buildTools.GetDeclaration(ctx, accVar.Declaration, useVar: true)}{accVar.Name} = {value};");
            if (!useEmpty
                && !isOptimizedOwnershipAccumulator
                && ctx.RootContext.IsThreadSafeEnabled
                && accVar.InstanceType.AllInterfaces.Any(i =>
                    symbolNames.GetGlobalName(i) == Names.IAccumulatorTypeName))
            {
                if (!ctx.RootContext.Root.IsStatic)
                {
                    ctx.RootContext.LockIsInUse = true;
                }

                var lockName = ctx.RootContext.Root.IsStatic ? Names.PerResolveLockFieldName : Names.LockFieldName;
                ctx.Lines.AppendLine($"(({Names.IAccumulatorTypeName}){accVar.Name}).Initialize({lockName});");
            }

            accVar.Declaration.IsDeclared = true;
            accVar.IsCreated = true;
        }
    }

    private int GetEagerAccumulatedResourceCount(
        DependencyGraph graph,
        IDependencyNode targetNode,
        ImmutableArray<MdAccumulator> accumulatorMetadata)
    {
        var count = 0;
        var path = new HashSet<IDependencyNode>();
        var sharedNodes = new HashSet<IDependencyNode>();
        var nodes = new Stack<(IDependencyNode Node, bool Exit)>();
        nodes.Push((targetNode, false));
        while (nodes.TryPop(out var item))
        {
            var node = item.Node;
            if (item.Exit)
            {
                path.Remove(node);
                continue;
            }

            if (path.Contains(node))
            {
                continue;
            }

            if (!ReferenceEquals(node, targetNode))
            {
                if (GetBoundaryAccumulators(graph, node).Any()
                    || nodeTools.IsLazy(node.Node, graph))
                {
                    continue;
                }

                if (node.ActualLifetime != Lifetime.Transient
                    && !sharedNodes.Add(node))
                {
                    continue;
                }
            }

            path.Add(node);
            nodes.Push((node, true));
            if (node.Arg is null
                && !IsOwnershipInfrastructure(node.Node.Type)
                && accumulatorMetadata.Any(i =>
                    node.ActualLifetime == i.Lifetime
                    && IsAssignableTo(node.Node.Type, i.Type)))
            {
                count++;
            }

            if (!graph.Graph.TryGetInEdges(node.Node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                nodes.Push((dependency.Source, false));
            }
        }

        return count;
    }

    private bool HasAccumulatedResources(
        DependencyGraph graph,
        IDependencyNode targetNode,
        MdAccumulator accumulator)
    {
        var processed = new HashSet<IDependencyNode>();
        var nodes = new Stack<IDependencyNode>();
        nodes.Push(targetNode);
        while (nodes.TryPop(out var node))
        {
            if (!processed.Add(node))
            {
                continue;
            }

            if (!ReferenceEquals(node, targetNode)
                && GetBoundaryAccumulators(graph, node).Any())
            {
                continue;
            }

            if (node.Arg is null
                && node.ActualLifetime == accumulator.Lifetime
                && IsAssignableTo(node.Node.Type, accumulator.Type)
                && (!IsOwnershipInfrastructure(node.Node.Type)
                    || IsAccumulatorExposedToUserGraph(graph, node)))
            {
                return true;
            }

            if (!graph.Graph.TryGetInEdges(node.Node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                nodes.Push(dependency.Source);
            }
        }

        return false;
    }

    private bool IsAccumulatorExposedToUserGraph(
        DependencyGraph graph,
        IDependencyNode node) =>
        IsBuiltInOwnershipAccumulator(node.Node.Type)
        && graph.Graph.TryGetOutEdges(node.Node, out var consumers)
        && consumers.Any(i => !IsOwnershipInfrastructure(i.Target.Type));

    private bool IsOwnershipInfrastructure(ITypeSymbol type) =>
        IsBuiltInOwnershipAccumulator(type)
        || type is INamedTypeSymbol namedType
        && namedType.AllInterfaces.Any(i =>
        {
            var interfaceName = symbolNames.GetGlobalName(i);
            return interfaceName is Names.IOwnedTypeName or Names.AbstractionsIOwnTypeName;
        });

    private bool IsBuiltInOwnershipAccumulator(ITypeSymbol type)
    {
        var typeName = symbolNames.GetGlobalName(type);
        return typeName is Names.OwnedTypeName or Names.AbstractionsOwnTypeName;
    }

    private IEnumerable<(MdAccumulator, Dependency)> GetBranchAccumulators(
        DependencyGraph graph,
        Dependency dependency)
    {
        if (dependency.Source.Construct is
            {
                Source:
                {
                    Kind: MdConstructKind.Accumulator,
                    State: IEnumerable<MdAccumulator> mdAccumulators
                }
            })
        {
            foreach (var accumulator in mdAccumulators)
            {
                yield return (accumulator, dependency);
            }

            yield break;
        }

        foreach (var accumulator in GetAccumulators(graph, dependency.Source))
        {
            yield return accumulator;
        }
    }

    private bool IsAssignableTo(ITypeSymbol type, ITypeSymbol contract)
    {
        if (typeSymbolComparer.RuntimeEquals(type, contract))
        {
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (namedType.AllInterfaces.Any(i => typeSymbolComparer.RuntimeEquals(i, contract)))
        {
            return true;
        }

        var baseType = namedType.BaseType;
        while (baseType is not null)
        {
            if (typeSymbolComparer.RuntimeEquals(baseType, contract))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}
