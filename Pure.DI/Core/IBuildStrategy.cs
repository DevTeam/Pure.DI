namespace Pure.DI.Core;

internal interface IBuildStrategy
{
    // ReSharper disable once UnusedParameter.Global
    ExpressionSyntax? TryBuild(Dependency dependency, SemanticType resolvingType);
}