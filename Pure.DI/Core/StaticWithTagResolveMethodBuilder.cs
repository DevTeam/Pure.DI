namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly ISyntaxRegistry _syntaxRegistry;

    public StaticWithTagResolveMethodBuilder(ISyntaxRegistry syntaxRegistry) =>
        _syntaxRegistry = syntaxRegistry;

    public ResolveMethod Build()
    {
        var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

        var keyVar = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxRepo.TagTypeTypeSyntax)
                .AddVariables(
                    SyntaxFactory.VariableDeclarator("key")
                        .WithInitializer(SyntaxFactory.EqualsValueClause(key))));

        var methodBlock = SyntaxFactory.Block()
            .AddStatements(keyVar)
            .AddStatements(_syntaxRegistry.FindMethod(nameof(ResolversByTagTable), nameof(ResolversByTagTable.Resolve)).Body!.Statements.ToArray());

        return new ResolveMethod(
            SyntaxRepo.StaticResolveWithTagMethodSyntax.AddBodyStatements(methodBlock.Statements.ToArray()));
    }
}