namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FallbackStrategy : IDefaultValueStrategy
    {
        internal const string CannotResolveMessage = "Cannot resolve an instance of the required type.";
        private static readonly ExpressionSyntax CannotResolveException = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.ArgumentException"))
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

            var rewriter = new FallbackRewriter(typeExpression, tagExpression);
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
                defaultExpression = SyntaxFactory.CastExpression(targetType, SyntaxFactory.ParenthesizedExpression(defaultExpression));
            }

            return SyntaxFactory.ReturnStatement(SyntaxFactory.ParenthesizedExpression(defaultExpression));
        }
    }
}
