namespace Pure.DI.Core.Code;

class LocalFunctions(INodeTools nodeTools): ILocalFunctions
{
    public bool UseFor(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        return  ctx is { HasOverrides: false, Accumulators.Length: 0 }
                && nodeTools.IsBlock(var.AbstractNode)
                && ctx.RootContext.Graph.Graph.TryGetOutEdges(var.Declaration.Node.Node, out var targets)
                && targets.Count > 1;
    }
}