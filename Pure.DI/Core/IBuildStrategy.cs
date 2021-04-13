namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBuildStrategy
    {
        ExpressionSyntax Build(Dependency dependency);
    }
}