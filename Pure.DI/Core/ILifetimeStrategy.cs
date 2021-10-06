namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    
    internal interface ILifetimeStrategy
    {
        Lifetime Lifetime { get; }
        
        ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression);
    }
}
