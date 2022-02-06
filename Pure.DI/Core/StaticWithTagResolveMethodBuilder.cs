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
        var tagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).FullName.ReplaceNamespace());
        var key = SyntaxFactory.ObjectCreationExpression(tagTypeTypeSyntax)
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

        var keyVar = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(tagTypeTypeSyntax)
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