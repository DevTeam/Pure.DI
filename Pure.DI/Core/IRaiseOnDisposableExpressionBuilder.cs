namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IRaiseOnDisposableExpressionBuilder
    {
        ExpressionSyntax Build(SemanticType type, Lifetime lifetime, ExpressionSyntax instanceExpression);
    }
}