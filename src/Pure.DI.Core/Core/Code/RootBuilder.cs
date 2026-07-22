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
    ISymbolNames symbolNames,
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
            .CreateAccumulators(rootContext.Graph, rootContext.Root.Node, accumulators.GetAccumulators(rootContext.Graph, rootContext.Root.Node), rootVarsMap)
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
        var body = new Lines();
        ctx = ctx with { Lines = body };
        BuildCode(ctx);
        rootVarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, rootVarInjection);
        var rollbackAccumulators = rootAccumulators
            .Where(i => !i.IsEmpty)
            .Select(i => i.VarInjection.Var)
            .Reverse()
            .Concat(rootContext.ConstructionFailureAccumulators.AsEnumerable().Reverse())
            .GroupBy(i => i.Name)
            .Select(i => i.First())
            .Where(CanRollback)
            .ToImmutableArray();
        if (rollbackAccumulators.IsEmpty)
        {
            lines.AppendLines(body);
        }
        else
        {
            lines.AppendLine("try");
            using (lines.CreateBlock())
            {
                lines.AppendLines(body);
                lines.AppendLine($"return {rootVarInjection.Var.CodeExpression};");
            }

            lines.AppendLine("catch");
            using (lines.CreateBlock())
            {
                foreach (var accumulator in rollbackAccumulators)
                {
                    AddRollback(lines, accumulator);
                }

                lines.AppendLine("throw;");
            }

            rootContext.ReturnWasAdded = true;
        }

        var setup = rootContext.Graph.Source;
        AddPerResolveVars(rootContext.Lines, rootVarsMap.Declarations.Where(i => i.Node.ActualLifetime is PerResolve), setup);
        rootContext.Lines.AppendLines(lines);
        return rootVarInjection;
    }

    private bool CanRollback(Var accumulator) =>
        Implements(accumulator.InstanceType, Names.IDisposableTypeName)
        || Implements(accumulator.InstanceType, Names.IAsyncDisposableTypeName);

    private void AddRollback(Lines lines, Var accumulator)
    {
        if (Implements(accumulator.InstanceType, Names.IDisposableTypeName))
        {
            AddDispose(
                lines,
                accumulator,
                $"(({Names.IDisposableTypeName}){accumulator.Name}).Dispose();");
            return;
        }

        if (!Implements(accumulator.InstanceType, Names.IAsyncDisposableTypeName))
        {
            return;
        }

        lines.AppendLine(new Line(int.MinValue, "#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER"));
        AddDispose(
            lines,
            accumulator,
            $"(({Names.IAsyncDisposableTypeName}){accumulator.Name}).DisposeAsync().GetAwaiter().GetResult();");
        lines.AppendLine(new Line(int.MinValue, "#endif"));
    }

    private static void AddDispose(Lines lines, Var accumulator, string disposeStatement)
    {
        if (accumulator.InstanceType.IsReferenceType)
        {
            lines.AppendLine($"if (!{Names.ObjectTypeName}.ReferenceEquals({accumulator.Name}, null))");
            using (lines.CreateBlock())
            {
                AddDisposeCore(lines, disposeStatement);
            }

            return;
        }

        AddDisposeCore(lines, disposeStatement);
    }

    private static void AddDisposeCore(Lines lines, string disposeStatement)
    {
        lines.AppendLine("try");
        using (lines.CreateBlock())
        {
            lines.AppendLine(disposeStatement);
        }

        lines.AppendLine("catch");
        using (lines.CreateBlock())
        {
            lines.AppendLine("// Preserve the original graph construction exception.");
        }
    }

    private bool Implements(ITypeSymbol type, string interfaceTypeName) =>
        symbolNames.GetGlobalName(type) == interfaceTypeName
        || type is INamedTypeSymbol namedType
        && namedType.AllInterfaces.Any(i => symbolNames.GetGlobalName(i) == interfaceTypeName);

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
