namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FactoryWrapperStrategy : IWrapperStrategy
    {
        private readonly IBuildContext _buildContext;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<IBuildStrategy> _buildStrategy;
        
        public FactoryWrapperStrategy(
            IBuildContext buildContext,
            IDiagnostic diagnostic,
            [Tag(Tags.SimpleBuildStrategy)] Func<IBuildStrategy> buildStrategy)
        {
            _buildContext = buildContext;
            _diagnostic = diagnostic;
            _buildStrategy = buildStrategy;
        }

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var factoryDependencyType = dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.IFactory`1")?.Construct(dependency.Implementation.Type);
            if (factoryDependencyType == null)
            {
                var error = $"Cannot resolve a factory for {dependency.Implementation}.";
                _diagnostic.Error(Diagnostics.Error.CannotResolveLifetime, error);
                throw new HandledException(error);
            }

            var factoryTypeDescriptions = _buildContext.TypeResolver.Resolve(new SemanticType(factoryDependencyType, dependency.Implementation))
                .Where(i => Equals(i.Tag?.ToString(), dependency.Tag?.ToString()));
            return factoryTypeDescriptions.Aggregate(objectBuildExpression, Wrap);
        }

        private ExpressionSyntax Wrap(ExpressionSyntax objectBuildExpression, Dependency factoryTypeDescription)
        {
            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
            return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _buildStrategy().Build(factoryTypeDescription, factoryTypeDescription.Implementation), SyntaxFactory.IdentifierName(nameof(IFactory<object>.Create))))
                .AddArgumentListArguments(SyntaxFactory.Argument(lambda));
        }
    }
}