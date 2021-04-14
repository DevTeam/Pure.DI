namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IFallbackStrategy
    {
        StatementSyntax Build(
            SemanticModel semanticModel,
            TypeSyntax? targetType,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression);
    }
}