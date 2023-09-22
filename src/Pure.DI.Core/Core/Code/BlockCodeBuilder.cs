// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class BlockCodeBuilder: ICodeBuilder<Block>
{
    public void Build(BuildContext ctx, in Block block)
    {
        var variable = ctx.Variable;
        if (!TryCreate(ctx, variable))
        {
            return;
        }

        var toCheckExistence = variable.Node.Lifetime != Lifetime.Transient;
        var level = ctx.Level;
        if (toCheckExistence)
        {
            var checkExpression = variable.InstanceType.IsValueType switch
            {
                true => $"!{variable.VarName}Created",
                false => $"{Names.SystemNamespace}Object.ReferenceEquals({variable.VarName}, null)"
            };
            
            if (ctx.IsThreadSafe)
            {
                ctx.Code.AppendLine($"if ({checkExpression})");
                ctx.Code.AppendLine("{");
                ctx.Code.IncIndent();
                ctx.Code.AppendLine($"lock ({Names.DisposablesFieldName})");
                ctx.Code.AppendLine("{");
                ctx.Code.IncIndent();
            }
            
            ctx.Code.AppendLine($"if ({checkExpression})");
            ctx.Code.AppendLine("{");
            ctx.Code.IncIndent();
            level++;
        }
        
        foreach (var statement in block.Statements)
        {
            ctx.StatementBuilder.Build(ctx with { Level = level, Variable = statement.Current }, statement);
        }

        if (!toCheckExistence)
        {
            return;
        }

        if (variable.Node.Lifetime == Lifetime.Singleton && ctx.BuildTools.IsDisposable(variable))
        {
            ctx.Code.AppendLine($"{Names.DisposablesFieldName}[{Names.DisposeIndexFieldName}++] = {variable.VarName};");
        }
            
        if (variable.InstanceType.IsValueType)
        {
            ctx.Code.AppendLine($"{variable.VarName}Created = true;");
            ctx.Code.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
        }
            
        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        if (!ctx.IsThreadSafe)
        {
            return;
        }
        
        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        ctx.Code.DecIndent();
        ctx.Code.AppendLine("}");
        ctx.Code.AppendLine();
    }
    
    private static bool TryCreate(BuildContext ctx, Variable variable)
    {
        // A transient instance must be created each time
        if (variable.Node.Lifetime == Lifetime.Transient)
        {
            return true;
        }

        if (ctx.Level >= variable.Info.Level)
        {
            return false;
        }
        
        variable.Info.Level = ctx.Level;
        return true;
    }
}