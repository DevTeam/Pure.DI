namespace Pure.DI.Core;

internal interface IBuildStrategy
{
    // ReSharper disable once UnusedParameter.Global
    Optional<ExpressionSyntax> TryBuild(Dependency dependency, SemanticType resolvingType);
}