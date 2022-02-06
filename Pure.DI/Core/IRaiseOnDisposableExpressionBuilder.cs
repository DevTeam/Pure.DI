namespace Pure.DI.Core;

using NS35EBD81B;

internal interface IRaiseOnDisposableExpressionBuilder
{
    ExpressionSyntax Build(SemanticType type, Lifetime lifetime, ExpressionSyntax instanceExpression);
}