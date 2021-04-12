namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ILifetimeStrategy
    {
        Lifetime Lifetime { get; }

        ExpressionSyntax Build(TypeDescription typeDescription, ExpressionSyntax objectBuildExpression);
    }
}
