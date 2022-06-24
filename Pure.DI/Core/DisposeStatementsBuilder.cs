// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class DisposeStatementsBuilder : IDisposeStatementsBuilder
{
    public IEnumerable<StatementSyntax> Build(MemberAccessExpressionSyntax instanceExpression, ExpressionSyntax hasInstanceExpression)
    {
        yield return SyntaxFactory.IfStatement(
            hasInstanceExpression,
            SyntaxFactory.Block().AddStatements(
                SyntaxRepo.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        hasInstanceExpression,
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                SyntaxRepo.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.CastExpression(
                                    SyntaxRepo.DisposableTypeSyntax,
                                    instanceExpression)
                                ),
                            SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose)))
                    ).AddArgumentListArguments())
            )
        );
    }
}