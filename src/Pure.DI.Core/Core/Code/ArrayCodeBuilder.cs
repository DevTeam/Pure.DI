namespace Pure.DI.Core.Code;

using System.Collections;

sealed class ArrayCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    IVariableTools variableTools)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        var varInjection = ctx.VarInjection;
        var var = varInjection.Var;
        var lines = ctx.Lines;
        var construct = var.AbstractNode.Construct!;
        var setup = ctx.RootContext.Graph.Source;
        if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var arrayDependencies))
        {
            var injections = new List<VarInjection>(arrayDependencies.Count);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var dependency in arrayDependencies)
            {
                injections.Add(ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source));
            }

            injections.Sort(variableTools.InjectionComparer);
            foreach (var dependencyVar in injections)
            {
                yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
                varInjections.Add(dependencyVar);
            }
        }

        var newArray = $"new {typeResolver.Resolve(setup, construct.Source.ElementType)}[{varInjections.Count.ToString()}] {{ {string.Join(", ", varInjections.Select(item => buildTools.OnInjected(ctx, item)))} }}";
        var onArrayCreated = buildTools.OnCreated(ctx, varInjection);
        if (onArrayCreated.Count > 0)
        {
            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {newArray};");
            lines.AppendLines(onArrayCreated);
        }
        else
        {
            var.CodeExpression =  newArray;
        }
    }
}
