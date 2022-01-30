namespace Pure.DI.Core;

internal interface ILifetimeStrategy
{
    Lifetime Lifetime { get; }

    ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression);
}