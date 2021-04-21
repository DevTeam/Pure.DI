namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TransientLifetimeStrategy : ILifetimeStrategy
    {
        public TransientLifetimeStrategy(Lifetime lifetime) => Lifetime = lifetime;

        public Lifetime Lifetime { get; }

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression) =>
            objectBuildExpression;
    }
}