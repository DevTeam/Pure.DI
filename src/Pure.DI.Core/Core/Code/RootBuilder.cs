// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable InvertIf
// ReSharper disable MergeIntoPattern
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code;

using System.Collections;
using static Lifetime;

class RootBuilder(
    IAccumulators accumulators,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory)
    : IFastBuilder<RootContext, VarInjection>
{
    public static readonly ParameterSyntax DefaultCtxParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127"));

    public VarInjection Build(in RootContext rootContext)
    {
        var rootVarsMap = rootContext.VarsMap;
        var rootVarInjection = rootVarsMap.GetInjection(rootContext.Graph, rootContext.Root.Injection, rootContext.Root.Node);
        var lines = new Lines();
        var rootAccumulators = accumulators
            .CreateAccumulators(rootContext.Graph, accumulators.GetAccumulators(rootContext.Graph, rootContext.Root.Node), rootVarsMap)
            .ToImmutableArray();
        var ctx = new CodeContext(
            rootContext,
            ImmutableArray<VarInjection>.Empty,
            rootVarInjection,
            rootContext.VarsMap,
            rootContext.IsThreadSafeEnabled,
            lines,
            rootAccumulators,
            []);

        accumulators.BuildAccumulators(ctx);
        if (rootAccumulators.Length == 0)
        {
            BuildCode(ctx);
        }
        else
        {
            lines.AppendLine("try");
            using (lines.CreateBlock())
            {
                BuildCode(ctx);
                rootVarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, rootVarInjection);
                lines.AppendLine($"return {rootVarInjection.Var.CodeExpression};");
            }

            lines.AppendLine("catch");
            using (lines.CreateBlock())
            {
                var accumulatorIndex = 0;
                var rollbackAccumulators = rootAccumulators
                    .Select(i => i.VarInjection.Var)
                    .Reverse()
                    .Concat(rootContext.ConstructionFailureAccumulators.AsEnumerable().Reverse())
                    .GroupBy(i => i.Name)
                    .Select(i => i.First());
                foreach (var accumulator in rollbackAccumulators)
                {
                    var accumulatorName = accumulator.Name;
                    var disposableName = $"disposableAccumulator{accumulatorIndex}";
                    lines.AppendLine($"if (({Names.ObjectTypeName}){accumulatorName} is {Names.IDisposableTypeName} {disposableName})");
                    using (lines.CreateBlock())
                    {
                        AddDispose(lines, $"{disposableName}.Dispose();");
                    }

                    lines.AppendLine(new Line(int.MinValue, "#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER"));
                    var asyncDisposableName = $"asyncDisposableAccumulator{accumulatorIndex++}";
                    lines.AppendLine($"else if (({Names.ObjectTypeName}){accumulatorName} is {Names.IAsyncDisposableTypeName} {asyncDisposableName})");
                    using (lines.CreateBlock())
                    {
                        AddDispose(lines, $"{asyncDisposableName}.DisposeAsync().GetAwaiter().GetResult();");
                    }

                    lines.AppendLine(new Line(int.MinValue, "#endif"));
                }

                lines.AppendLine("throw;");
            }

            rootContext.ReturnWasAdded = true;
        }

        rootVarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, rootVarInjection);

        var setup = rootContext.Graph.Source;
        AddPerResolveVars(rootContext.Lines, rootVarsMap.Declarations.Where(i => i.Node.ActualLifetime is PerResolve), setup);
        rootContext.Lines.AppendLines(lines);
        return rootVarInjection;

        static void AddDispose(Lines code, string disposeStatement)
        {
            code.AppendLine("try");
            using (code.CreateBlock())
            {
                code.AppendLine(disposeStatement);
            }

            code.AppendLine("catch");
            using (code.CreateBlock())
            {
                code.AppendLine("// Preserve the original graph construction exception.");
            }
        }
    }

    private void AddPerResolveVars(Lines lines, IEnumerable<VarDeclaration> perResolveVars, MdSetup setup)
    {
        var vars = perResolveVars as List<VarDeclaration> ?? perResolveVars.ToList();
        vars.Sort(static (x, y) => x.Node.BindingId.CompareTo(y.Node.BindingId));
        foreach (var perResolve in vars)
        {
            lines.AppendLine($"var {perResolve.Name} = default({typeResolver.Resolve(setup, perResolve.InstanceType)});");
            if (perResolve.InstanceType.IsValueType)
            {
                lines.AppendLine($"var {perResolve.Name}{Names.CreatedValueNameSuffix} = false;");
            }
        }
    }

    private void BuildCode(CodeContext parentCtx)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(variablesCodeBuilderFactory().Build(parentCtx));
        while (stack.Count > 0)
        {
            var top = stack.Peek();
            if (top.MoveNext())
            {
                if (top.Current is IEnumerator child)
                {
                    stack.Push(child);
                }
            }
            else
            {
                stack.Pop();
            }
        }
    }
}
