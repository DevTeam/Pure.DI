namespace Pure.DI.Core.Code;

using System.Collections;

sealed class ImplicitConversionCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    ITypes types)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        if (!ctx.RootContext.Graph.Graph.TryGetInEdges(ctx.VarInjection.Var.AbstractNode.Node, out var dependencies)
            || dependencies.Count != 1)
        {
            yield break;
        }

        var dependency = dependencies.First();
        var dependencyVar = ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source);
        yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
        varInjections.Add(dependencyVar);
        var source = buildTools.OnInjected(ctx, dependencyVar);
        if (!types.TypeEquals(dependencyVar.Var.InstanceType, dependency.Injection.Type))
        {
            var sourceType = typeResolver.Resolve(ctx.RootContext.Graph.Source, dependency.Injection.Type);
            source = $"({sourceType})({source})";
        }

        ctx.VarInjection.Var.CodeExpression = source;
    }
}
