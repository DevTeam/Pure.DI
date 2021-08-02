namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency);
    }
}