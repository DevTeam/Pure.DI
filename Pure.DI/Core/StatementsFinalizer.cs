// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class StatementsFinalizer : IStatementsFinalizer, IMembersBuilder
{
    private readonly IBuildContext _buildContext;

    public StatementsFinalizer(IBuildContext buildContext) => _buildContext = buildContext;

    public int Order => int.MaxValue;

    private bool IsActive => _buildContext.FinalizationStatements.Any();

    private static MemberKey DeepnessMemberKey => new("_deepness", typeof(StatementsFinalizer));

    private string DeepnessFieldName => _buildContext.NameService.FindName(DeepnessMemberKey);

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        if (IsActive)
        {
            yield return _buildContext.GetOrAddMember(DeepnessMemberKey, () =>
                SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("int"))
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(DeepnessFieldName)
                            )
                    )
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.ThreadStaticAttr))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
        }
    }

    public BlockSyntax? AddFinalizationStatements(BlockSyntax? block)
    {
        if (block == default || !IsActive)
        {
            return block;
        }

        var refToDeepness = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DeepnessFieldName)).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
        var incrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Increment"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var decrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Decrement"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var releaseBlock = SyntaxFactory.Block()
            .AddStatements(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, decrementStatement, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                    SyntaxFactory.Block().AddStatements(_buildContext.FinalizationStatements.ToArray())
                ));

        var tryStatement = SyntaxFactory.TryStatement(
            block,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(releaseBlock));

        return
            SyntaxFactory.Block()
                .AddStatements(SyntaxFactory.ExpressionStatement(incrementStatement))
                .AddStatements(tryStatement);
    }
}