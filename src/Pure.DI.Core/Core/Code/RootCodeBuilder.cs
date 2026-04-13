namespace Pure.DI.Core.Code;

using System.Collections;
using static CodeBuilderKind;
using static Lifetime;
using static LinesExtensions;

sealed class RootCodeBuilder(
    [Tag(Implementation)] IBuilder<CodeBuilderContext, IEnumerator> implementationBuilder,
    [Tag(Factory)] IBuilder<CodeBuilderContext, IEnumerator> factoryBuilder,
    [Tag(Construct)] IBuilder<CodeBuilderContext, IEnumerator> constructBuilder,
    IBuildTools buildTools,
    IAccumulators accumulators,
    ILocks locks,
    ICompilations compilations,
    IUniqueNameProvider uniqueNameProvider,
    INameFormatter nameFormatter,
    ILocalFunctions localFunctions,
    INodeTools nodeTools,
    IConstructors constructors)
    : IBuilder<CodeContext, IEnumerator>
{
    public IEnumerator Build(CodeContext parentCtx)
    {
        var (var, injection) = parentCtx.VarInjection;
        if (var.IsCreated)
        {
            if (parentCtx.IsFactory && !string.IsNullOrWhiteSpace(var.LocalFunctionName))
            {
                parentCtx.Lines.AppendLine($"{var.LocalFunctionName}();");
            }

            yield break;
        }

        if (!string.IsNullOrEmpty(var.LocalFunctionName))
        {
            if (parentCtx.IsFactory)
            {
                parentCtx.Lines.AppendLine($"{var.LocalFunctionName}();");
            }
            else if (!var.IsLocalFunctionCalled)
            {
                parentCtx.Lines.AppendLine($"{var.LocalFunctionName}();");
                var.IsLocalFunctionCalled = true;
            }
            var.IsCreated = true;
            var.Declaration.IsDeclared = true;
            yield break;
        }

        var lines = new Lines();
        var currentTag = ReferenceEquals(injection.Tag, MdTag.ContextTag) ? parentCtx.ContextTag : injection.Tag;
        var varCtx = parentCtx with
        {
            Lines = lines,
            ContextTag = currentTag
        };

        var varsMap = varCtx.VarsMap;
        var isBlock = nodeTools.IsBlock(var.AbstractNode);
        var isLazy = nodeTools.IsLazy(var.AbstractNode.Node, parentCtx.RootContext.Graph);
        var acc = isLazy ? accumulators.GetAccumulators(varCtx.RootContext.Graph, var.AbstractNode).ToImmutableArray() : ImmutableArray<(MdAccumulator, Dependency)>.Empty;
        var isLocalFunction = localFunctions.UseFor(varCtx);
        var mapToken =
            isLocalFunction
                ? varsMap.LocalFunction(var, lines)
                : isLazy
                    ? varsMap.Lazy(var, lines)
                    : isBlock
                        ? varsMap.Block(var, lines)
                        : Disposables.Empty;

        if (isLocalFunction || isLazy)
        {
            varCtx = varCtx with { IsLockRequired = varCtx.RootContext.IsThreadSafeEnabled };
        }

        if (isBlock)
        {
            StartSingleInstanceCheck(varCtx with { Lines = lines });
            varCtx = varCtx with { IsLockRequired = false };
        }

        var ctx = varCtx;
        if (isLazy)
        {
            ctx = ctx with { Accumulators = ctx.Accumulators.AddRange(accumulators.CreateAccumulators(varCtx.RootContext.Graph, acc, varsMap)), IsFactory = false };
            ctx.Overrides.Clear();
            accumulators.BuildAccumulators(ctx);
        }

        var varInjections = new List<VarInjection>();
        var.IsCreated = true;

        var builder = var.AbstractNode.Node switch
        {
            { Implementation: not null } => implementationBuilder.Build(new CodeBuilderContext(ctx, varInjections)),
            { Factory: not null } => factoryBuilder.Build(new CodeBuilderContext(ctx, varInjections)),
            _ => var.AbstractNode.Construct is not null ? constructBuilder.Build(new CodeBuilderContext(ctx, varInjections)) : null
        };

        if (builder is not null)
        {
            while (builder.MoveNext())
            {
                yield return builder.Current;
            }
        }

        if (isBlock)
        {
            FinishSingleInstanceCheck(varCtx with { IsLockRequired = parentCtx.IsLockRequired });
        }

        mapToken.Dispose();

        if (isLocalFunction)
        {
            var baseName = nameFormatter.Format("{type}{tag}", varCtx.VarInjection.Var.InstanceType, currentTag);
            var localFunction = var.LocalFunction;
            if (compilations.GetLanguageVersion(varCtx.RootContext.Graph.Source.SemanticModel.Compilation) >= LanguageVersion.CSharp9)
            {
                buildTools.AddAggressiveInlining(localFunction);
            }

            var.LocalFunctionName = uniqueNameProvider.GetUniqueName($"Ensure{baseName}Exists{Names.Salt}");
            localFunction.AppendLine($"void {var.LocalFunctionName}()");
            using (localFunction.CreateBlock())
            {
                localFunction.AppendLines(lines);
            }

            lines = new Lines();
            lines.AppendLine($"{var.LocalFunctionName}();");
        }

        parentCtx.Lines.AppendLines(lines);
        var.Declaration.IsDeclared = true;
    }

    private void StartSingleInstanceCheck(CodeContext ctx)
    {
        var isLockRequired = ctx.IsLockRequired;
        var var = ctx.VarInjection.Var;
        var lines = ctx.Lines;
        var compilation = var.AbstractNode.Binding.SemanticModel.Compilation;
        var checkExpression = var.InstanceType.IsValueType
            ? $"!{var.Name}{Names.CreatedValueNameSuffix}"
            : buildTools.NullCheck(compilation, var.Name);

        lines.AppendLine($"if ({checkExpression})");
        if (isLockRequired)
        {
            lines.IncIndent();
            locks.AddLockStatements(ctx.RootContext.Root.IsStatic, lines, false);
            lines.IncIndent();
            lines.AppendLine($"if ({checkExpression})");
        }

        lines.AppendLine(BlockStart);
        lines.IncIndent();
    }

    private void FinishSingleInstanceCheck(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        var lines = ctx.Lines;
        if (var.AbstractNode.ActualLifetime is Singleton or Scoped && nodeTools.IsDisposableAny(var.AbstractNode.Node))
        {
            var parent = "";
            if (var.AbstractNode.ActualLifetime == Singleton && constructors.IsEnabled(ctx.RootContext.Graph))
            {
                parent = string.IsNullOrWhiteSpace(ctx.RootContext.Graph.Source.Hints.ScopeMethodName)
                    ? $"{Names.RootFieldName}."
                    : $"{Names.RootVarName}.";
            }

            lines.AppendLine($"{parent}{Names.DisposablesFieldName}[{parent}{Names.DisposeIndexFieldName}++] = {var.Name};");
        }

        if (var.InstanceType.IsValueType)
        {
            if (var.AbstractNode.ActualLifetime is not Transient and not PerBlock && ctx.RootContext.IsThreadSafeEnabled)
            {
                lines.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            }

            lines.AppendLine($"{var.Name}{Names.CreatedValueNameSuffix} = true;");
        }

        lines.DecIndent();
        lines.AppendLine(BlockFinish);
        if (ctx.IsLockRequired)
        {
            lines.DecIndent();
            lines.DecIndent();
        }

        lines.AppendLine();
    }
}
