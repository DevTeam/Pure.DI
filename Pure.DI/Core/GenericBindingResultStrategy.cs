namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class GenericBindingResultStrategy : IBindingResultStrategy
    {
        public ExpressionSyntax Build(ExpressionSyntax objectExpression) =>
            SyntaxFactory.CastExpression(ResolverBuilder.TTypeSyntax, SyntaxFactory.CastExpression(ResolverBuilder.ObjectTypeSyntax, SyntaxFactory.ParenthesizedExpression(objectExpression)));
    }
}