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
        private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
        private readonly Func<IBuildStrategy> _buildStrategy;
        
        public FactoryWrapperStrategy(
            IBuildContext buildContext,
            ICannotResolveExceptionFactory cannotResolveExceptionFactory,
            Func<IBuildStrategy> buildStrategy)
        {
            _buildContext = buildContext;
            _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
            _buildStrategy = buildStrategy;
        }

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var factoryDependencyType = dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.IFactory`1")?.Construct(dependency.Implementation.Type);
            if (factoryDependencyType == null)
            {
                throw _cannotResolveExceptionFactory.Create(dependency.Binding);
            }

            var factoryTypeDescriptions = _buildContext.TypeResolver.Resolve(new SemanticType(factoryDependencyType, dependency.Implementation))
                .Where(i => Equals(i.Tag?.ToString(), dependency.Tag?.ToString()));
            return factoryTypeDescriptions.Aggregate(objectBuildExpression, Wrap);
        }

        private ExpressionSyntax Wrap(ExpressionSyntax objectBuildExpression, Dependency factoryTypeDescription)
        {
            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
            var instance = _buildStrategy().TryBuild(factoryTypeDescription, factoryTypeDescription.Implementation);
            if (instance != null)
            {
                return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, SyntaxFactory.IdentifierName(nameof(IFactory<object>.Create))))
                    .AddArgumentListArguments(SyntaxFactory.Argument(lambda));
            }

            throw _cannotResolveExceptionFactory.Create(factoryTypeDescription.Binding);
        }
    }
}