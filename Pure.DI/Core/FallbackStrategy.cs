namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FallbackStrategy : IFallbackStrategy
    {
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IObjectBuilder _objectBuilder;
        
        public FallbackStrategy(
            ITypeResolver typeResolver,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.AutowiringBuilder)] IObjectBuilder objectBuilder)
        {
            _typeResolver = typeResolver;
            _buildStrategy = buildStrategy;
            _objectBuilder = objectBuilder;
        }

        public ExpressionSyntax Build(SemanticModel semanticModel)
        {
            ExpressionSyntax? fallback = null;
            var fallbackType = semanticModel.Compilation.GetTypeByMetadataName(typeof(IFallback).ToString());
            if (fallbackType != null)
            {
                var fallbackDependency = _typeResolver.Resolve(new SemanticType(fallbackType, semanticModel), null, true, true);
                if (fallbackDependency.IsResolved)
                {
                    var fallbackInstance = _objectBuilder.Build(_buildStrategy, fallbackDependency);
                    fallback = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, fallbackInstance, SyntaxFactory.IdentifierName(nameof(IFallback.Resolve))))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")), SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));
                }
            }

            if (fallback == null)
            {
                return SyntaxFactory.DefaultExpression(SyntaxRepo.FuncTypeObjectObjectTypeSyntax);
            }

            return SyntaxFactory.ParenthesizedLambdaExpression(fallback)
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("type")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")));
        }
    }
}
