namespace Pure.DI.Core
{
    using IoC;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FallbackStrategy : IFallbackStrategy
    {
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IObjectBuilder _objectBuilder;
        private const string CannotResolveMessage = "Cannot resolve an instance of the required type.";
        // ReSharper disable once InconsistentNaming
        private static readonly ExpressionSyntax CannotResolveExpression = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.ArgumentException"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList().AddArguments(
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression).WithToken(SyntaxFactory.Literal(CannotResolveMessage)))));

        public FallbackStrategy(
            ITypeResolver typeResolver,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.AutowiringBuilder)] IObjectBuilder objectBuilder)
        {
            _typeResolver = typeResolver;
            _buildStrategy = buildStrategy;
            _objectBuilder = objectBuilder;
        }

        public StatementSyntax Build(
            SemanticModel semanticModel,
            TypeSyntax? targetType,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression)
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
                        .AddArgumentListArguments(SyntaxFactory.Argument(typeExpression), SyntaxFactory.Argument(tagExpression));
                }
            }

            if (fallback == null)
            {
                return SyntaxFactory.ThrowStatement().WithExpression(CannotResolveExpression);
            }

            ExpressionSyntax defaultExpression = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                fallback,
                SyntaxFactory.ThrowExpression(CannotResolveExpression));

            if (targetType != null)
            {
                defaultExpression = SyntaxFactory.CastExpression(targetType, SyntaxFactory.ParenthesizedExpression(defaultExpression));
            }

            return SyntaxFactory.ReturnStatement(SyntaxFactory.ParenthesizedExpression(defaultExpression));
        }
    }
}
