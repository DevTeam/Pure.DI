// ReSharper disable All
namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    class AsIsBindingResultStrategy : IBindingResultStrategy
    {
        public ExpressionSyntax Build(ExpressionSyntax objectExpression) =>
            SyntaxFactory.ParenthesizedExpression(objectExpression);
    }
}