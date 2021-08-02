// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FactoryMethodWrapperStrategy : IWrapperStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly IBuildContext _buildContext;
        private readonly Func<IBuildStrategy> _buildStrategy;
        private readonly IIncludeTypeFilter _includeTypeFilter;

        public FactoryMethodWrapperStrategy(
            IDiagnostic diagnostic,
            IBuildContext buildContext,
            Func<IBuildStrategy> buildStrategy,
            IIncludeTypeFilter includeTypeFilter)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _buildStrategy = buildStrategy;
            _includeTypeFilter = includeTypeFilter;
        }

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            if (dependency.Implementation.ImplementsInterface<IFactory>())
            {
                return objectBuildExpression;
            }

            var factoryType = _buildContext.Compilation.GetTypeByMetadataName("Pure.DI.IFactory");
            if (factoryType == null)
            {
                const string error = "Factory is not supported.";
                _diagnostic.Error(Diagnostics.Error.Unsupported, error);
                throw new HandledException(error);
            }

            var factoryTypeDescriptions = _buildContext.TypeResolver.Resolve(new SemanticType(factoryType, dependency.Implementation));
            return factoryTypeDescriptions.Aggregate(objectBuildExpression, (syntax, factoryTypeDescription) => Wrap(dependency, syntax, factoryTypeDescription));
        }
        
        private ExpressionSyntax Wrap(Dependency dependency, ExpressionSyntax objectBuildExpression, Dependency factoryTypeDescription)
        {
            if (!_includeTypeFilter.IsAccepted(factoryTypeDescription.Implementation, dependency.Implementation))
            {
                return objectBuildExpression;
            }

            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
            ExpressionSyntax? tagExpression = dependency.Tag?.ToLiteralExpression();
            var factoryExpression = _buildStrategy().TryBuild(factoryTypeDescription, factoryTypeDescription.Implementation);
            if (factoryExpression == null)
            {
                var error = $"Cannot resolve factory {factoryTypeDescription}.";
                _diagnostic.Error(Diagnostics.Error.CannotResolve, error);
                throw new HandledException(error);
            }

            return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, factoryExpression, SyntaxFactory.GenericName(nameof(IFactory.Create)).AddTypeArgumentListArguments(dependency.Implementation)))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(lambda),
                    SyntaxFactory.Argument(tagExpression ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)));
        }
    }
}