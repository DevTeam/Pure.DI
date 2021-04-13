namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ILifetimeStrategy
    {
        Lifetime Lifetime { get; }

        ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression);
    }
}
