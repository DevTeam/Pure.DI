namespace Pure.DI.Core.Code;

class Accumulators(
    INodeTools nodeTools,
    IBuildTools buildTools)
    : IAccumulators
{
    public IEnumerable<(MdAccumulator, Dependency)> GetAccumulators(
        IGraph<DependencyNode, Dependency> graph,
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

            if (!graph.TryGetInEdges(node.Node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (nodeTools.IsLazy(source.Node))
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

    public IEnumerable<Accumulator> CreateAccumulators(
        DependencyGraph graph,
        Root root,
        IEnumerable<(MdAccumulator accumulator, Dependency dependency)> accumulators,
        IVarsMap varsMap) =>
        accumulators.Select(i => new Accumulator(varsMap.GetInjection(graph, root, i.dependency.Injection, i.dependency.Source), i.accumulator.Type, i.accumulator.Lifetime));

    public void BuildAccumulators(CodeContext ctx)
    {
        var accVars = ctx.Accumulators.Select(accumulator => accumulator.VarInjection.Var).Where(accVar => !accVar.Declaration.IsDeclared);
        foreach (var accVar in accVars)
        {
            ctx.Lines.AppendLine($"{buildTools.GetDeclaration(ctx, accVar.Declaration, useVar: true)}{accVar.Name} = new {accVar.InstanceType}();");
            accVar.Declaration.IsDeclared = true;
            accVar.IsCreated = true;
        }
    }
}