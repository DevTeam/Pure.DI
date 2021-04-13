namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TransientLifetimeStrategy : ILifetimeStrategy
    {
        public Lifetime Lifetime => Lifetime.Transient;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression) =>
            objectBuildExpression;
    }
}