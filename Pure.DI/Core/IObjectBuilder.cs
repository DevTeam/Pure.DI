namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ExpressionSyntax Build(IBuildStrategy buildStrategy, Dependency dependency);
    }
}