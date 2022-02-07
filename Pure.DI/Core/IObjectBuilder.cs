namespace Pure.DI.Core;

internal interface IObjectBuilder
{
    Optional<ExpressionSyntax> TryBuild(IBuildStrategy buildStrategy, Dependency dependency);
}