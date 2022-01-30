namespace Pure.DI.Core;

internal interface IDisposeStatementsBuilder
{
    IEnumerable<StatementSyntax> Build(MemberAccessExpressionSyntax instanceExpression, ExpressionSyntax hasInstanceExpression);
}