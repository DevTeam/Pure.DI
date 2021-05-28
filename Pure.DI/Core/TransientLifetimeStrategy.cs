namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TransientLifetimeStrategy : ILifetimeStrategy
    {
        private readonly IRaiseOnDisposableExpressionBuilder _raiseOnDisposableExpressionBuilder;

        public TransientLifetimeStrategy(IRaiseOnDisposableExpressionBuilder raiseOnDisposableExpressionBuilder) =>
            _raiseOnDisposableExpressionBuilder = raiseOnDisposableExpressionBuilder;

        public Lifetime Lifetime => Lifetime.Transient;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression) =>
            _raiseOnDisposableExpressionBuilder.Build(dependency.Implementation, Lifetime.Transient, objectBuildExpression);
    }
}