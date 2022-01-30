namespace Pure.DI.Core;

internal interface IObjectBuilder
{
    ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency);
}