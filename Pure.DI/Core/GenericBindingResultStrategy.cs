namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class GenericBindingResultStrategy : IBindingResultStrategy
{
    public ExpressionSyntax Build(ExpressionSyntax objectExpression) =>
        SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, SyntaxFactory.CastExpression(SyntaxRepo.ObjectTypeSyntax, objectExpression));
}