namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericBindingResultStrategy : IBindingResultStrategy
    {
        public ExpressionSyntax Build(ExpressionSyntax objectExpression) =>
            SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, SyntaxFactory.CastExpression(SyntaxRepo.ObjectTypeSyntax, SyntaxFactory.ParenthesizedExpression(objectExpression)));
    }
}