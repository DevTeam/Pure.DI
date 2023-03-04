namespace Pure.DI.Core;

internal sealed class FactoryBlockRewriter: CSharpSyntaxRewriter
{
    private readonly Variable _target;
    private readonly string _finishMark;
    private int _processed;

    public FactoryBlockRewriter(Variable target, string finishMark)
    {
        _target = target;
        _finishMark = finishMark;
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        _processed++;
        return base.VisitSimpleLambdaExpression(node);
    }

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        _processed++;
        return base.VisitParenthesizedLambdaExpression(node);
    }

    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        if (_processed == 0 && node.Expression is {} returnBody)
        {
            return SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(_target.Name), 
                        (ExpressionSyntax)Visit(returnBody))),
                    SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(_finishMark))
                );
        }
        
        return base.VisitReturnStatement(node);
    }
}