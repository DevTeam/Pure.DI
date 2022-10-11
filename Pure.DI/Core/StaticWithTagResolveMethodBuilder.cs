namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly ISyntaxRegistry _syntaxRegistry;
    private readonly ISettings _settings;

    public StaticWithTagResolveMethodBuilder(ISyntaxRegistry syntaxRegistry, ISettings settings)
    {
        _syntaxRegistry = syntaxRegistry;
        _settings = settings;
    }

    public ResolveMethod Build()
    {
        var tagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).FullName.ReplaceNamespace());
        var key = SyntaxRepo.ObjectCreationExpression(tagTypeTypeSyntax)
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

        var keyVar = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(tagTypeTypeSyntax)
                .AddVariables(
                    SyntaxFactory.VariableDeclarator("key")
                        .WithSpace()
                        .WithInitializer(SyntaxFactory.EqualsValueClause(key))));

        var methodBlock = SyntaxFactory.Block()
            .AddStatements(keyVar)
            .AddStatements(_syntaxRegistry.FindMethod(nameof(ResolversByTagTable), nameof(ResolversByTagTable.Resolve)).Body!.Statements.ToArray());

        return new ResolveMethod(
            SyntaxRepo.CreateStaticResolveWithTagMethodSyntax(_settings.AccessibilityToken).AddBodyStatements(methodBlock.Statements.ToArray()));
    }
}