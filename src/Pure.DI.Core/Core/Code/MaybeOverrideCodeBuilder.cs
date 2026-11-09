namespace Pure.DI.Core.Code;

using System.Collections;
using static LinesExtensions;

/// <summary>
/// Builds a value that a lambda on a cycle may override for all the levels below it, see #192.
/// The code reads the overridden value where a lambda above it overrides the value, creates the value where none does,
/// and checks whether the value is overridden in a local function, whose callers may differ in it.
/// </summary>
sealed class MaybeOverrideCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    INameProvider nameProvider,
    IUniqueNameProvider uniqueNameProvider)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        var var = ctx.VarInjection.Var;
        // An override without a dependency is the overridden value itself, which is named by its contract.
        if (var.AbstractNode.Construct is not { Source.State: DpOverride @override }
            || !ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var dependencies)
            || dependencies.Count != 1)
        {
            yield break;
        }

        var name = nameProvider.GetOverrideVariableName(@override.Source);
        var isVisible = ctx.VisibleOverrides.TryGetValue(name, out var presence);
        if (isVisible && presence is null)
        {
            var.CodeExpression = name;
            yield break;
        }

        var dependency = dependencies.First();
        var dependencyVar = ctx.VarsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source);
        if (!isVisible)
        {
            yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
            varInjections.Add(dependencyVar);
            var.CodeExpression = buildTools.OnInjected(ctx, dependencyVar);
            yield break;
        }

        var lines = ctx.Lines;
        var valueName = uniqueNameProvider.GetUniqueName($"{name}OrDefault");
        lines.AppendLine($"{typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)} {valueName};");
        lines.AppendLine($"if ({presence})");
        lines.AppendLine(BlockStart);
        lines.IncIndent();
        lines.AppendLine($"{valueName} = {name};");
        lines.DecIndent();
        lines.AppendLine(BlockFinish);
        lines.AppendLine("else");
        lines.AppendLine(BlockStart);
        lines.IncIndent();
        // The values created in the branch are not seen after it.
        using (ctx.VarsMap.Lazy(var, lines, ImmutableArray<int>.Empty))
        {
            yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(dependencyVar));
            varInjections.Add(dependencyVar);
            lines.AppendLine($"{valueName} = {buildTools.OnInjected(ctx, dependencyVar)};");
        }

        lines.DecIndent();
        lines.AppendLine(BlockFinish);
        var.CodeExpression = valueName;
    }

    // The name of the parameter of a local function that tells whether its caller overrides the value.
    public static string GetPresenceName(string overrideName) =>
        $"has{char.ToUpperInvariant(overrideName[0])}{overrideName.Substring(1)}";
}
