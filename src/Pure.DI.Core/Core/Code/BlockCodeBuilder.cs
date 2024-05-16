// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class BlockCodeBuilder(
    INodeInfo nodeInfo)
    : ICodeBuilder<Block>
{
    public void Build(BuildContext ctx, in Block block)
    {
        var variable = ctx.Variable;
        if (!IsNewInstanceRequired(variable))
        {
            return;
        }
        
        var localMethodName = $"{Names.EnsureExistsMethodNamePrefix}_{variable.VariableDeclarationName}".Replace("__", "_");
        var info = block.Current.Info;
        if (info.HasLocalMethod)
        {
            ctx.Code.AppendLine($"{localMethodName}();");
            return;
        }
        
        var code = new LinesBuilder();
        try
        {
            var level = ctx.Level;
            var isThreadSafe = ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled;
            var lockIsRequired = ctx.LockIsRequired ?? isThreadSafe;
            var toCheckExistence =
                // The "singleton" or "scoped" instance must be created with a check each time
                variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped
                || variable.Node.Lifetime == Lifetime.Scoped
                // The "per resolve" instance should be created without checks if it is the only one in the composition
                || (variable.Node.Lifetime == Lifetime.PerResolve && variable.Info.RefCount > 1);

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
                ctx = ctx with { Accumulators = accumulators.ToImmutableArray() };
            }

            if (toCheckExistence)
            {
                var checkExpression = variable.InstanceType.IsValueType
                    ? $"!{variable.VariableName}Created"
                    : $"{variable.VariableName} == null";

                if (lockIsRequired)
                {
                    code.AppendLine($"if ({checkExpression})");
                    code.AppendLine("{");
                    code.IncIndent();
                    code.AppendLine($"lock ({Names.LockFieldName})");
                    code.AppendLine("{");
                    code.IncIndent();
                    ctx = ctx with { LockIsRequired = false };
                }

                code.AppendLine($"if ({checkExpression})");
                code.AppendLine("{");
                code.IncIndent();
                ctx = ctx with { Level = level + 1 };
            }
            
            foreach (var statement in block.Statements)
            {
                ctx.StatementBuilder.Build(ctx with { Variable = statement.Current, Code = code }, statement);
            }
            
            if (!toCheckExistence)
            {
                return;
            }
            
            if (variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped && nodeInfo.IsDisposable(variable.Node))
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
            code.AppendLine("}");
            if (!lockIsRequired)
            {
                code.AppendLine();
                return;
            }

            code.DecIndent();
            code.AppendLine("}");
            code.DecIndent();
            code.AppendLine("}");
            code.AppendLine();
        }
        finally
        {
            if (block.Parent is not null
                && info is { PerBlockRefCount: > 1 }
                && code.Count > 11) 
            {
                var localMethodCode = ctx.LocalFunctionsCode;
                if (variable.Node.Binding.SemanticModel.Compilation.GetLanguageVersion() >= LanguageVersion.CSharp9)
                {
                    localMethodCode.AppendLine($"[{Names.MethodImplAttributeName}({Names.MethodImplAggressiveInlining})]");
                }
                
                localMethodCode.AppendLine($"void {localMethodName}()");
                localMethodCode.AppendLine("{");
                using (localMethodCode.Indent())
                {
                    localMethodCode.AppendLines(code.Lines);
                }
                
                localMethodCode.AppendLine("}");
                code = new LinesBuilder();
                code.AppendLine($"{localMethodName}();");
                info.HasLocalMethod = true;
            }
            
            ctx.Code.AppendLines(code.Lines);
        }
    }
    
    private static bool IsNewInstanceRequired(Variable variable)
    {
        if (variable.Node.Lifetime == Lifetime.Transient)
        {
            return true;
        }

        if (variable.Current.HasCycle)
        {
            return false;
        }
        
        var owners = variable
            .GetPath()
            .Skip(1)
            .TakeWhile(i => !i.Current.IsLazy)
            .OfType<Block>()
            .ToArray();

        if (variable.Info.Owners.Intersect(owners).Any())
        {
            return false;
        }

        variable.Info.Owners.Add(owners.FirstOrDefault());
        return true;
    }
}