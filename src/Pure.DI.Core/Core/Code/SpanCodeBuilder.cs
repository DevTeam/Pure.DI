// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core.Code;

using System.Collections;

sealed class SpanCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    ICompilations compilations,
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
        var count = 0;
        if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var spanDependencies))
        {
            var injections = new List<VarInjection>(spanDependencies.Count);
            foreach (var dependency in spanDependencies)
            {
                injections.Add(ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source));
            }

            injections.Sort(variableTools.InjectionComparer);
            foreach (var dependencyVar in injections)
            {
                yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
                varInjections.Add(dependencyVar);
            }

            count = spanDependencies.Count;
        }

        var createArray = $"{typeResolver.Resolve(setup, construct.Source.ElementType)}[{varInjections.Count.ToString()}] {{ {string.Join(", ", varInjections.Select(item => buildTools.OnInjected(ctx, item)))} }}";

        var isStackalloc =
            construct.Source.ElementType.IsValueType
            && count <= Const.MaxStackalloc
            && compilations.GetLanguageVersion(construct.Binding.SemanticModel.Compilation) >= LanguageVersion.CSharp7_3;

        var newSpan = isStackalloc ? $"stackalloc {createArray}" : $"new {Names.SystemNamespace}Span<{typeResolver.Resolve(setup, construct.Source.ElementType)}>(new {createArray})";
        var onSpanCreated = buildTools.OnCreated(ctx, varInjection);
        if (onSpanCreated.Count > 0)
        {
            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name} = {newSpan};");
            lines.AppendLines(onSpanCreated);
        }
        else
        {
            var.CodeExpression =  newSpan;
        }
    }
}
