namespace Pure.DI.Core.Code;

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
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap) =>
        accumulators.Select(i => new Accumulator(varsMap.GetInjection(graph, i.dependency.Injection, i.dependency.Source), i.accumulator.Type, i.accumulator.Lifetime));

    public void BuildAccumulators(CodeContext ctx, bool includeDeclared = false)
    {
        var accVars = ctx.Accumulators.Select(accumulator => accumulator.VarInjection.Var).Where(accVar => !accVar.Declaration.IsDeclared);
        if (includeDeclared)
        {
            accVars = ctx.Accumulators
                .Select(accumulator => accumulator.VarInjection.Var)
                .GroupBy(accVar => accVar.Name)
                .Select(group => group.First());
        }

        foreach (var accVar in accVars)
        {
            ctx.Lines.AppendLine($"{buildTools.GetDeclaration(ctx, accVar.Declaration, useVar: true)}{accVar.Name} = new {accVar.InstanceType}();");
            if (ctx.RootContext.IsThreadSafeEnabled
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
