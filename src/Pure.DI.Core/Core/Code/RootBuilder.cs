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
        var rootVarInjection = rootVarsMap.GetInjection(rootContext.Graph, rootContext.Root, rootContext.Root.Injection, rootContext.Root.Node);
        var lines = new Lines();
        var ctx = new CodeContext(
            rootContext,
            ImmutableArray<VarInjection>.Empty,
            rootVarInjection,
            rootContext.VarsMap,
            rootContext.IsThreadSafeEnabled,
            lines,
            accumulators.CreateAccumulators(rootContext.Graph, rootContext.Root, accumulators.GetAccumulators(rootContext.Graph, rootContext.Root.Node), rootVarsMap).ToImmutableArray(),
            []);

        accumulators.BuildAccumulators(ctx);
        BuildCode(ctx);
        rootVarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, rootVarInjection);

        var setup = rootContext.Graph.Source;
        var perResolves = new List<VarDeclaration>();
        foreach (var declaration in rootVarsMap.Declarations)
        {
            if (declaration.Node.ActualLifetime is PerResolve)
            {
                perResolves.Add(declaration);
            }
        }

        perResolves.Sort((a, b) => a.Node.BindingId.CompareTo(b.Node.BindingId));

        foreach (var perResolve in perResolves)
        {
            rootContext.Lines.AppendLine($"var {perResolve.Name} = default({typeResolver.Resolve(setup, perResolve.InstanceType)});");
            if (perResolve.InstanceType.IsValueType)
            {
                rootContext.Lines.AppendLine($"var {perResolve.Name}{Names.CreatedValueNameSuffix} = false;");
            }
        }

        rootContext.Lines.AppendLines(lines);
        return rootVarInjection;
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