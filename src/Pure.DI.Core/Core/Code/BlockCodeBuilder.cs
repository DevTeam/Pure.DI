// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static LinesBuilderExtensions;

sealed class BlockCodeBuilder(
    INodeInfo nodeInfo,
    ICompilations compilations,
    ILocks locks,
    IBuildTools buildTools)
    : ICodeBuilder<Block>
{
    public void Build(BuildContext ctx, in Block block)
    {
        var variable = ctx.Variable;
        var compilation = variable.Node.Binding.SemanticModel.Compilation;
        if (!IsNewInstanceRequired(variable))
        {
            return;
        }

        var localMethodName = $"{Names.EnsureExistsMethodNamePrefix}_{variable.VariableDeclarationName}".Replace("__", "_");
        var info = block.Current.Info;
        var code = new LinesBuilder();
        var isEmpty = false;
        try
        {
            var level = ctx.Level;
            var isThreadSafe = ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled;
            var lockIsRequired = ctx.LockIsRequired ?? isThreadSafe;
            var toCheckExistence = variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve;
            var uniqueAccumulators = ctx.Accumulators
                .Where(accumulator => !accumulator.IsDeclared)
                .GroupBy(i => i.Name)
                .Select(i => i.First());

            foreach (var accumulator in uniqueAccumulators)
            {
                code.AppendLine($"var {accumulator.Name} = new {accumulator.AccumulatorType}();");
            }

            var accumulators = ctx.Accumulators.Select(accumulator => accumulator with { IsDeclared = true }).ToList();
            if (accumulators.Count > 0)
            {
                ctx = ctx with { Accumulators = accumulators.ToImmutableArray(), AvoidLocalFunction = false};
            }

            if (toCheckExistence)
            {
                var checkExpression = variable.InstanceType.IsValueType
                    ? $"!{variable.VariableName}Created"
                    : buildTools.NullCheck(compilation, variable.VariableName);

                if (lockIsRequired)
                {
                    code.AppendLine($"if ({checkExpression})");
                    code.AppendLine(BlockStart);
                    code.IncIndent();
                    locks.AddLockStatements(ctx.DependencyGraph, code, false);
                    code.AppendLine(BlockStart);
                    code.IncIndent();
                    ctx = ctx with { LockIsRequired = false };
                }

                code.AppendLine($"if ({checkExpression})");
                code.AppendLine(BlockStart);
                code.IncIndent();
                ctx = ctx with { Level = level + 1 };
            }

            var content = new LinesBuilder();
            foreach (var statement in block.Statements)
            {
                ctx.StatementBuilder.Build(ctx with { Variable = statement.Current, Code = content, AvoidLocalFunction = false }, statement);
            }

            if (content.Count == 0)
            {
                isEmpty = true;
                return;
            }

            if (info.HasLocalMethod)
            {
                ctx.Code.AppendLine($"{localMethodName}();");
                isEmpty = true;
                return;
            }

            code.AppendLines(content.Lines);

            if (!toCheckExistence)
            {
                return;
            }

            if (variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped && nodeInfo.IsDisposableAny(variable.Node))
            {
                var parent = "";
                if (variable.Node.Lifetime == Lifetime.Singleton)
                {
                    parent = $"{Names.RootFieldName}.";
                }

                code.AppendLine($"{parent}{Names.DisposablesFieldName}[{parent}{Names.DisposeIndexFieldName}++] = {variable.VariableName};");
            }

            if (variable.InstanceType.IsValueType)
            {
                if (variable.Node.Lifetime is not Lifetime.Transient and not Lifetime.PerBlock && isThreadSafe)
                {
                    code.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
                }

                code.AppendLine($"{variable.VariableName}Created = true;");
            }

            code.DecIndent();
            code.AppendLine(BlockFinish);
            if (!lockIsRequired)
            {
                locks.AddUnlockStatements(ctx.DependencyGraph, code, false);
                code.AppendLine();
                return;
            }

            code.DecIndent();
            code.AppendLine(BlockFinish);
            code.DecIndent();
            code.AppendLine(BlockFinish);
            code.AppendLine();
        }
        finally
        {
            if (!isEmpty)
            {
                if (!ctx.AvoidLocalFunction
                    && block.Parent is not null
                    && info is { PerBlockRefCount: > 1 }
                    && code.Count >= ctx.DependencyGraph.Source.Hints.LocalFunctionLines)
                {
                    var localMethodCode = ctx.LocalFunctionsCode;
                    if (compilations.GetLanguageVersion(compilation) >= LanguageVersion.CSharp9)
                    {
                        buildTools.AddAggressiveInlining(localMethodCode);
                    }

                    localMethodCode.AppendLine($"void {localMethodName}()");
                    localMethodCode.AppendLine(BlockStart);
                    using (localMethodCode.Indent())
                    {
                        localMethodCode.AppendLines(code.Lines);
                    }

                    localMethodCode.AppendLine(BlockFinish);
                    code = new LinesBuilder();
                    code.AppendLine($"{localMethodName}();");
                    info.HasLocalMethod = true;
                }

                ctx.Code.AppendLines(code.Lines);
            }
        }
    }

    private static bool IsNewInstanceRequired(Variable variable) =>
        variable.Node.Lifetime == Lifetime.Transient
        || !variable.Current.HasCycle;
}