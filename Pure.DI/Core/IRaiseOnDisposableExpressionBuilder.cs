namespace Pure.DI.Core;

internal interface IRaiseOnDisposableExpressionBuilder
{
    ExpressionSyntax Build(SemanticType type, Lifetime lifetime, ExpressionSyntax instanceExpression);
}