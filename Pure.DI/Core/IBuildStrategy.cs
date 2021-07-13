namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBuildStrategy
    {
        // ReSharper disable once UnusedParameter.Global
        ExpressionSyntax Build(Dependency dependency, SemanticType resolvingType);
    }
}