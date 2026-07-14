namespace Pure.DI.Core.Code;

using System.Collections;

sealed class ImplicitConversionCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    IBuildTools buildTools)
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
        ctx.VarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, dependencyVar);
    }
}
