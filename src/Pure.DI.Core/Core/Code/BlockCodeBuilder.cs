namespace Pure.DI.Core.Code;

internal class BlockCodeBuilder: ICodeBuilder<Block>
{
    public void Build(BuildContext ctx, in Block block)
    {
        var variable = ctx.Variable;
        if (!TryCreate(variable))
        {
            return;
        }
        
        var toCheckExistence = variable.Node.Lifetime != Lifetime.Transient;
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
        }

        foreach (var statement in block.Statements)
        {
            ctx.StatementBuilder.Build(ctx with{ Variable = statement.Current }, statement);
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
    
    private static bool TryCreate(Variable variable)
    {
        // A transient instance must be created each time
        if (variable.Node.Lifetime == Lifetime.Transient)
        {
            return true;
        }

        // An instance has been created in some parent block
        if (variable.GetPath().OfType<Block>().Any(i => i.Created.Contains(variable.Node.Binding)))
        {
            return false;
        }

        // An instance has already been created in the current block
        var block = variable.GetPath().OfType<Block>().First();
        return block.Created.Add(variable.Node.Binding);
    }
}