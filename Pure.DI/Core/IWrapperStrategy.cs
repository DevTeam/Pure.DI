namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IWrapperStrategy
    {
        ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression);
    }
}