// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class BlockCodeBuilder: ICodeBuilder<Block>
{
    public void Build(BuildContext ctx, in Block block)
    {
        var variable = ctx.Variable;
        if (!IsNewInstanceRequired(ctx, variable))
        {
            return;
        }

        var level = ctx.Level;
        var isThreadSafe = ctx.DependencyGraph.Source.Hints.GetHint(Hint.ThreadSafe, SettingState.On) == SettingState.On;
        var lockIsRequired = ctx.LockIsRequired ?? isThreadSafe;
        var toCheckExistence =
            // The "singleton" instance must be created with a check each time
            variable.Node.Lifetime == Lifetime.Singleton
            // The "per resolve" instance should be created without checks if it is the only one in the composition
            || (variable.Node.Lifetime == Lifetime.PerResolve && variable.Info.RefCount > 1);
        
        if (toCheckExistence)
        {
            var checkExpression = variable.InstanceType.IsValueType
                ? $"!{variable.VariableName}Created"
                : $"object.ReferenceEquals({variable.VariableName}, null)";

            if (lockIsRequired)
            {
                ctx.Code.AppendLine($"if ({checkExpression})");
                ctx.Code.AppendLine("{");
                ctx.Code.IncIndent();
                ctx.Code.AppendLine($"lock ({Names.DisposablesFieldName})");
                ctx.Code.AppendLine("{");
                ctx.Code.IncIndent();
                ctx = ctx with { LockIsRequired = false };
            }
            
            ctx.Code.AppendLine($"if ({checkExpression})");
            ctx.Code.AppendLine("{");
            ctx.Code.IncIndent();
            ctx = ctx with { Level = level + 1 };
        }

        foreach (var statement in block.Statements)
        {
            ctx.StatementBuilder.Build(ctx with { Variable = statement.Current }, statement);
        }

        if (!toCheckExistence)
        {
            return;
        }

        if (variable.Node.Lifetime == Lifetime.Singleton && variable.Node.IsDisposable())
        {
            ctx.Code.AppendLine($"{Names.DisposablesFieldName}[{Names.DisposeIndexFieldName}++] = {variable.VariableName};");
        }

        if (variable.InstanceType.IsValueType)
        {
            if (variable.Node.Lifetime != Lifetime.Transient && isThreadSafe)
            {
                ctx.Code.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            }

            ctx.Code.AppendLine($"{variable.VariableName}Created = true;");
        }

        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        if (!lockIsRequired)
        {
            return;
        }
        
        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        ctx.Code.AppendLine();
    }
    
    private static bool IsNewInstanceRequired(BuildContext ctx, Variable variable)
    {
        // A transient instance must be created each time
        if (variable.Node.Lifetime == Lifetime.Transient)
        {
            return true;
        }
        
        // Do not create an instance if it has already been created at this level or a level below it
        if (ctx.Level >= variable.Info.Level)
        {
            return false;
        }
        
        variable.Info.Level = ctx.Level;
        return true;
    }
}