// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class StatementsBlockWrapperMembersBuilder : IStatementsBlockWrapper, IMembersBuilder
{
    private readonly IBuildContext _buildContext;

    public StatementsBlockWrapperMembersBuilder(IBuildContext buildContext) => _buildContext = buildContext;

    public int Order => int.MaxValue;

    private bool IsActive => _buildContext.FinalizationStatements.Any();

    private static MemberKey DeepnessMemberKey => new("_deepness", typeof(StatementsBlockWrapperMembersBuilder));

    private string DeepnessFieldName => _buildContext.NameService.FindName(DeepnessMemberKey);

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        if (!IsActive)
        {
            yield break;
        }
        
        yield return _buildContext.GetOrAddMember(DeepnessMemberKey, () =>
            SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("int"))
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(DeepnessFieldName).WithSpace()
                        )
                )
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.ThreadStaticAttr))
                .AddModifiers(
                    SyntaxKind.PrivateKeyword.WithSpace(),
                    SyntaxKind.StaticKeyword.WithSpace()));
    }

    public BlockSyntax? AddFinalizationStatements(BlockSyntax? block)
    {
        if (block == default || !IsActive)
        {
            return block;
        }

        var refToDeepness = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DeepnessFieldName)).WithRefKindKeyword(SyntaxKind.RefKeyword.WithSpace());
        var incrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Increment"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var decrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Decrement"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var finallyBlock = SyntaxFactory.Block()
            .AddStatements(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, decrementStatement, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                    SyntaxFactory.Block().AddStatements(_buildContext.FinalizationStatements.ToArray())
                ));

        var tryStatement = SyntaxFactory.TryStatement(
            block,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(finallyBlock));

        return
            SyntaxFactory.Block()
                .AddStatements(SyntaxRepo.ExpressionStatement(incrementStatement))
                .AddStatements(tryStatement);
    }
}