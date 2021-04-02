namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class AsIsBindingResultStrategy : IBindingResultStrategy
    {
        public ExpressionSyntax Build(ExpressionSyntax objectExpression) =>
            SyntaxFactory.ParenthesizedExpression(objectExpression);
    }
}