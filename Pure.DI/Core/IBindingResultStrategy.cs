namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingResultStrategy
    {
        ExpressionSyntax Build(ExpressionSyntax objectExpression);
    }
}
