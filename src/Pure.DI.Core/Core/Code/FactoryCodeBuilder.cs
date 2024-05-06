// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable MergeIntoPattern
namespace Pure.DI.Core.Code;

internal class FactoryCodeBuilder(
    IIdGenerator idGenerator,
    INodeInfo nodeInfo,
    IArguments arguments)
    : ICodeBuilder<DpFactory>
{
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";

    public void Build(BuildContext ctx, in DpFactory factory)
    {
        var variable = ctx.Variable;
        var code = ctx.Code;
        var level = ctx.Level;
        var lockIsRequired = ctx.LockIsRequired;
        if (nodeInfo.IsLazy(variable.Node))
        {
            level++;
            lockIsRequired = default;
        }
        
        // Rewrites syntax tree
        var finishLabel = $"{variable.VariableDeclarationName}Finish";
        var injections = new List<FactoryRewriter.Injection>();
        var localVariableRenamingRewriter = new LocalVariableRenamingRewriter(idGenerator, factory.Source.SemanticModel);
        var factoryExpression = localVariableRenamingRewriter.Rewrite(factory.Source.Factory);
        var factoryRewriter = new FactoryRewriter(arguments, factory, variable, finishLabel, injections);
        var lambda = factoryRewriter.Rewrite(factoryExpression);
        new FactoryValidator(factory).Validate(lambda); 
        SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
        if (syntaxNode is not BlockSyntax)
        {
            code.Append($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = ");
        }
        else
        {
            if (!variable.IsDeclared)
            {
                code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName};");
            }
        }

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

        var injectionsCtx = ctx;
        if (variable.IsLazy && variable.Node.Accumulators.Count > 0)
        {
            injectionsCtx = injectionsCtx with
            {
                Accumulators = injectionsCtx.Accumulators.AddRange(
                    variable.Node.Accumulators
                        .Select(accumulator => accumulator with { IsDeclared = false }))
            };
        }
        
        var indent = new Indent(0);
        var text = syntaxNode.GetText();
        foreach (var textLine in text.Lines)
        {
            var line = text.ToString(textLine.Span);
            if (line.Contains(InjectionStatement) && resolvers.MoveNext())
            {
                // When an injection marker
                var (injection, argument) = resolvers.Current;
                using (code.Indent(indent.Value))
                {
                    ctx.StatementBuilder.Build(injectionsCtx with { Level = level, Variable = argument.Current, LockIsRequired = lockIsRequired }, argument);
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