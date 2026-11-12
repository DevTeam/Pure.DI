namespace Pure.DI.Core.Code;

using System.Collections;
using static LinesExtensions;

sealed class EnumerableCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    ICompilations compilations,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    IVariableTools variableTools,
    ILocalFunctions localFunctions,
    IAccumulators accumulators)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    private const string SharedKeyPrefix = "enumeration:";

    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        var varInjection = ctx.VarInjection;
        var var = varInjection.Var;
        var lines = ctx.Lines;
        var construct = var.AbstractNode.Construct!;
        var setup = ctx.RootContext.Graph.Source;

        // An enumeration that reads nothing of the code around it is declared once, at the level of its root,
        // so the code of different lambdas, or of a branch of a factory and the code after it, calls the same function, see #201.
        var rootScope = ctx.RootContext.LocalFunctionScope;
        // The accumulators that the enumeration declares itself are not a part of the key.
        var sharedKey = IsShared(ctx) ? SharedKeyPrefix + localFunctions.GetKey(ctx with { Accumulators = ImmutableArray<Accumulator>.Empty }) : null;
        if (sharedKey is not null && rootScope.TryGetName(sharedKey, out var sharedName))
        {
            var.CodeExpression = $"{sharedName}()";
            AppendOnCreated(ctx, lines, $"{sharedName}()");
            yield break;
        }

        var localMethodName = $"{Names.EnumerateMethodNamePrefix}_{var.Declaration.Name}".Replace("__", "_");
        var newEnum = $"{localMethodName}()";
        // A cyclic element can request this enumerable while its iterator body is being generated.
        var.CodeExpression = newEnum;
        var functionLines = sharedKey is not null ? new Lines() : lines;
        var functionCtx = sharedKey is not null
            ? ctx with { Lines = functionLines, LocalFunctionScope = rootScope }
            : ctx;

        if (compilations.GetLanguageVersion(construct.Source.SemanticModel.Compilation) >= LanguageVersion.CSharp9)
        {
            buildTools.AddAggressiveInlining(functionLines);
        }

        var methodPrefix = construct.Source.Kind == MdConstructKind.AsyncEnumerable ? "async " : "";
        functionLines.AppendLine($"{methodPrefix}{typeResolver.Resolve(setup, var.InstanceType)} {localMethodName}()");
        using (functionLines.CreateBlock())
        {
            // The enumeration is deferred like a lambda, so each pass over it is a block of its own,
            // and the accumulators isolated by it, such as the Owned of an element that injects IOwned, are created in it.
            var elementCtx = functionCtx;
            if (ctx.Accumulators.Any(i => !i.VarInjection.Var.Declaration.IsDeclared))
            {
                accumulators.BuildAccumulators(functionCtx);
                // The local functions that refer to these accumulators are declared in the body of the enumeration.
                elementCtx = functionCtx with { LocalFunctionScope = new LocalFunctionScope(ctx.Accumulators) };
            }

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
                    yield return variablesCodeBuilderFactory().Build(elementCtx.CreateChild(dependencyVar));
                    functionLines.AppendLine($"yield return {buildTools.OnInjected(functionCtx, dependencyVar)};");
                    hasYieldReturn = true;
                }
            }

            if (methodPrefix == "async ")
            {
                functionLines.AppendLine("await Task.CompletedTask;");
            }
            else
            {
                if (!hasYieldReturn)
                {
                    functionLines.AppendLine("yield break;");
                }
            }

            if (!ReferenceEquals(elementCtx.LocalFunctionScope, functionCtx.LocalFunctionScope))
            {
                foreach (var localFunction in elementCtx.LocalFunctionScope.Functions)
                {
                    functionLines.AppendLine();
                    functionLines.AppendLines(localFunction);
                }
            }
        }

        if (sharedKey is not null)
        {
            rootScope.Add(sharedKey, localMethodName, functionLines);
        }
        else
        {
            lines.AppendLine();
        }

        AppendOnCreated(ctx, lines, newEnum);
    }

    private void AppendOnCreated(CodeContext ctx, Lines lines, string newEnum)
    {
        var var = ctx.VarInjection.Var;
        var onEnumCreated = buildTools.OnCreated(ctx, ctx.VarInjection);
        if (onEnumCreated.Count > 0)
        {
            // The creation hook may replace the enumerable, so later injections must use the local.
            var.CodeExpression = var.Name;
            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {newEnum};");
            lines.AppendLines(onEnumCreated);
        }
    }

    // The code of the enumeration refers only to what the whole root sees: the fields, the arguments of the root,
    // and the local functions of the root. The elements are created inside the enumeration, see VarsMap.Lazy,
    // except an instance on a cycle, which the enumeration gets from the code around it.
    // A value overridden by a lambda and an accumulator that the code around the enumeration declares are not seen
    // at the level of the root, while the accumulators isolated by the enumeration are declared in it.
    private static bool IsShared(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        return var.HasCycle != true
               && ctx.Accumulators.All(i => !i.VarInjection.Var.Declaration.IsDeclared)
               && ctx.DetachedAccumulators.IsDefaultOrEmpty
               && !(ctx.RootContext.UseSites.FreeOverrides.TryGetValue(var.AbstractNode.BindingId, out var overrides) && !overrides.IsDefaultOrEmpty);
    }
}
