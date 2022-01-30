namespace Pure.DI.Core;

internal interface IWrapperStrategy
{
    ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression);
}