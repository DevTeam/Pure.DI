namespace Pure.DI.Core;

internal interface IBindingResultStrategy
{
    ExpressionSyntax Build(ExpressionSyntax objectExpression);
}