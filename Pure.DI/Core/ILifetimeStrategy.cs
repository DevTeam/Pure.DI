namespace Pure.DI.Core;

using NS35EBD81B;

internal interface ILifetimeStrategy
{
    Lifetime Lifetime { get; }

    ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression);
}