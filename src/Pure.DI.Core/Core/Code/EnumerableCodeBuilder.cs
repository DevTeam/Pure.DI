namespace Pure.DI.Core.Code;

using System.Collections;
using static LinesExtensions;

sealed class EnumerableCodeBuilder(
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
        var localMethodName = $"{Names.EnumerateMethodNamePrefix}_{var.Declaration.Name}".Replace("__", "_");
        if (compilations.GetLanguageVersion(construct.Source.SemanticModel.Compilation) >= LanguageVersion.CSharp9)
        {
            buildTools.AddAggressiveInlining(lines);
        }

        var methodPrefix = construct.Source.Kind == MdConstructKind.AsyncEnumerable ? "async " : "";
        lines.AppendLine($"{methodPrefix}{typeResolver.Resolve(setup, var.InstanceType)} {localMethodName}()");
        using (lines.CreateBlock())
        {
            var hasYieldReturn = false;
            if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var enumerableDependencies))
            {
                var injections = new List<VarInjection>(enumerableDependencies.Count);
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var dependency in enumerableDependencies)
                {
                    injections.Add(ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source));
                }

                injections.Sort(variableTools.InjectionComparer);

                foreach (var dependencyVar in injections)
                {
                    varInjections.Add(dependencyVar);
                    yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
                    lines.AppendLine($"yield return {buildTools.OnInjected(ctx, dependencyVar)};");
                    hasYieldReturn = true;
                }
            }

            if (methodPrefix == "async ")
            {
                lines.AppendLine("await Task.CompletedTask;");
            }
            else
            {
                if (!hasYieldReturn)
                {
                    lines.AppendLine("yield break;");
                }
            }
        }

        lines.AppendLine();
        var newEnum = $"{localMethodName}()";
        var onEnumCreated = buildTools.OnCreated(ctx, varInjection);
        if (onEnumCreated.Count > 0)
        {
            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {newEnum};");
            lines.AppendLines(onEnumCreated);
        }
        else
        {
            var.CodeExpression = newEnum;
        }
    }
}
