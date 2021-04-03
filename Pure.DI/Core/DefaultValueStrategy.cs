namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class DefaultValueStrategy : IDefaultValueStrategy
    {
        public ExpressionSyntax Build(
            ICollection<FactoryMetadata> metadata,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression,
            ExpressionSyntax defaultValue)
        {
            if (metadata.Count == 0)
            {
                return defaultValue;
            }

            var rewriter = new DefaultFactoryRewriter(typeExpression, tagExpression);
            var factories = metadata
                .Select(i => (ExpressionSyntax)rewriter.Visit(i.Factory))
                .Reverse()
                .Concat(new []{ defaultValue }).ToList();

            var exp = factories.Skip(1)
                .Aggregate(
                    factories.First(),
                    (source, acc) =>
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.CoalesceExpression,
                            source,
                            acc));

            return exp;
        }

        private class DefaultFactoryRewriter: CSharpSyntaxRewriter
        {
            private readonly ExpressionSyntax _typeExpression;
            private readonly ExpressionSyntax _tagExpression;

            public DefaultFactoryRewriter(ExpressionSyntax typeExpression, ExpressionSyntax tagExpression)
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
