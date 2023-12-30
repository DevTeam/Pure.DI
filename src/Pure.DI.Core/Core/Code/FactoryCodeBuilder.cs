// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class FactoryCodeBuilder(IIdGenerator idGenerator) : ICodeBuilder<DpFactory>
{
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";

    public void Build(BuildContext ctx, in DpFactory factory)
    {
        var variable = ctx.Variable;
        var code = ctx.Code;
        var level = ctx.Level;
        var lockIsRequired = ctx.LockIsRequired;
        if (variable.Node.IsLazy())
        {
            level++;
            lockIsRequired = default;
        }
        
        // Rewrites syntax tree
        var finishLabel = $"{variable.VariableName}Finish";
        var injections = new List<FactoryRewriter.Injection>();
        var localVariableRenamingRewriter = new LocalVariableRenamingRewriter(idGenerator, factory.Source.SemanticModel);
        var factoryExpression = localVariableRenamingRewriter.Rewrite(factory.Source.Factory);
        var factoryRewriter = new FactoryRewriter(factory, variable, finishLabel, injections);
         var lambda = factoryRewriter.Rewrite(factoryExpression);

        SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
        if (syntaxNode is not BlockSyntax)
        {
            code.Append($"{ctx.BuildTools.GetDeclaration(variable, true)}{variable.VariableName} = ");
        }
        else
        {
            if (!variable.IsDeclared)
            {
                code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable, true)}{variable.VariableName};");
            }
        }

        var lines = syntaxNode.ToString().Split('\n');
        
        // Replaces injection markers by injection code
        if (variable.Args.Count != injections.Count)
        {
            throw new CompileErrorException(
                $"{variable.Node.Lifetime} lifetime does not support cyclic dependencies.",
                factory.Source.Source.GetLocation(),
                LogId.ErrorInvalidMetadata);
        }
        
        using var resolvers = injections
            .Zip(variable.Args, (injection, argument) => (injection, argument))
            .GetEnumerator();
        
        var indent = new Indent(0);
        foreach (var line in lines)
        {
            if (line.Trim() == InjectionStatement && resolvers.MoveNext())
            {
                // When an injection marker
                var (injection, argument) = resolvers.Current;
                using (code.Indent(indent.Value))
                {
                    ctx.StatementBuilder.Build(ctx with { Level = level, Variable = argument.Current, LockIsRequired = lockIsRequired }, argument);
                    code.AppendLine($"{(injection.DeclarationRequired ? "var " : "")}{injection.VariableName} = {ctx.BuildTools.OnInjected(ctx, argument.Current)};");
                }
            }
            else
            {
                // When a code
                var len = 0;
                for (; len < line.Length && line[len] == ' '; len++)
                {
                }

                indent = len / Formatting.IndentSize;
                code.AppendLine(line);
            }
        }

        if (factoryRewriter.IsFinishMarkRequired)
        {
            code.AppendLine($"{finishLabel}:;");
        }
        
        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }
}