namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FallbackStrategy : IDefaultValueStrategy
    {
        internal const string CannotResolveMessage = "Cannot resolve an instance of the required type.";
        private static readonly ExpressionSyntax CannotResolveException = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(nameof(ArgumentException)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList().AddArguments(
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression).WithToken(SyntaxFactory.Literal(CannotResolveMessage)))));

        public StatementSyntax Build(
            ICollection<FallbackMetadata> metadata,
            TypeSyntax? targetType,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression)
        {
            if (metadata.Count == 0)
            {
                return SyntaxFactory.ThrowStatement().WithExpression(CannotResolveException);
            }

            var rewriter = new FallbackFactoryRewriter(typeExpression, tagExpression);
            var factories = metadata
                .Select(i => (ExpressionSyntax)rewriter.Visit(i.Factory))
                .Reverse()
                .Concat(new []{ SyntaxFactory.ThrowExpression(CannotResolveException) }).ToList();

            var defaultExpression = factories.Skip(1)
                .Aggregate(
                    factories.First(),
                    (source, acc) =>
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.CoalesceExpression,
                            source,
                            acc));


            if (targetType != null)
            {
                defaultExpression = SyntaxFactory.CastExpression(targetType, defaultExpression);
            }

            return SyntaxFactory.ReturnStatement(defaultExpression);
        }

        private class FallbackFactoryRewriter: CSharpSyntaxRewriter
        {
            private readonly ExpressionSyntax _typeExpression;
            private readonly ExpressionSyntax _tagExpression;

            public FallbackFactoryRewriter(ExpressionSyntax typeExpression, ExpressionSyntax tagExpression)
            {
                _typeExpression = typeExpression;
                _tagExpression = tagExpression;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                return SyntaxFactory.InvocationExpression(node)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(_typeExpression),
                        SyntaxFactory.Argument(_tagExpression));
            }

            public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                return base.VisitSimpleLambdaExpression(node);
            }
        }
    }
}
