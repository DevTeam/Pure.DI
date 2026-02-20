using Pure.DI.Core.Models;

namespace Pure.DI.Core.Code;

class LocalFunctions(INodeTools nodeTools): ILocalFunctions
{
    public bool UseFor(CodeContext ctx)
    {
        if (ctx.IsFactory && HasOverridesInDependencies(ctx))
        {
            return false;
        }

        var var = ctx.VarInjection.Var;
        return  ctx is { HasOverrides: false, Accumulators.Length: 0 }
                && nodeTools.IsBlock(var.AbstractNode)
                && ctx.RootContext.Graph.Graph.TryGetOutEdges(var.Declaration.Node.Node, out var targets)
                && targets.Count > 1;
    }

    private static bool HasOverridesInDependencies(CodeContext ctx)
    {
        var graph = ctx.RootContext.Graph.Graph;
        var visited = new HashSet<int>();
        var stack = new Stack<DependencyNode>();
        stack.Push(ctx.VarInjection.Var.AbstractNode.Node);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node.Binding.Id))
            {
                continue;
            }

            if (node.Factory?.HasOverrides == true)
            {
                return true;
            }

            if (!graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                if (dependency.Injection.Kind is InjectionKind.FactoryInjection or InjectionKind.Override)
                {
                    return true;
                }

                stack.Push(dependency.Source);
            }
        }

        return false;
    }
}
