namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FallbackStrategy : IFallbackStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IObjectBuilder _objectBuilder;
        
        public FallbackStrategy(
            IDiagnostic diagnostic,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy,
            [Tag(Tags.AutowiringBuilder)] IObjectBuilder objectBuilder)
        {
            _diagnostic = diagnostic;
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
                var fallbackDependency = _typeResolver.Resolve(new SemanticType(fallbackType, semanticModel), null, fallbackType.Locations, true, true);
                if (fallbackDependency.IsResolved)
                {
                    var fallbackInstance = _objectBuilder.TryBuild(_buildStrategy, fallbackDependency);
                    if (fallbackInstance == null)
                    {
                        var error = $"Cannot resolve {fallbackDependency}.";
                        _diagnostic.Error(Diagnostics.Error.CannotResolve, error);
                        throw new HandledException(error);
                    }

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
