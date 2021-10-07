namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
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

        public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var baseFactoryType = dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.IFactory`1");
            var factoryType = baseFactoryType?.Construct(resolvingType.Type);
            if (factoryType == null)
            {
                throw _cannotResolveExceptionFactory.Create(dependency.Binding, dependency.Tag,"a factory");
            }
            
            if (dependency.Implementation.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
            {
                var baseGenericType = baseFactoryType?.ConstructUnboundGenericType();
                if (namedTypeSymbol.Interfaces.Any(i => i.IsGenericType && i.ConstructUnboundGenericType().Equals(baseGenericType, SymbolEqualityComparer.Default)))
                {
                    return objectBuildExpression;
                }
            }

            var factoryTypes = _buildContext.TypeResolver.Resolve(new SemanticType(factoryType, dependency.Implementation))
                .Where(i => Equals(i.Tag?.ToString(), dependency.Tag?.ToString()));
            
           return factoryTypes.Aggregate(objectBuildExpression, (syntax, factoryDependency) => Wrap(syntax, factoryDependency, dependency));
        }

        private ExpressionSyntax Wrap(CSharpSyntaxNode objectBuildExpression, Dependency factoryDependency, Dependency dependency)
        {
            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
            var instance = _buildStrategy().TryBuild(factoryDependency, factoryDependency.Implementation);
            if (instance == null)
            {
                throw _cannotResolveExceptionFactory.Create(factoryDependency.Binding, factoryDependency.Tag, "a factory");
            }

            return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, SyntaxFactory.IdentifierName(nameof(IFactory<object>.Create))))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(lambda),
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependency.Implementation)),
                    SyntaxFactory.Argument(dependency.Tag ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)));

        }
    }
}