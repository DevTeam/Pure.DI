namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FallbackRewriter: CSharpSyntaxRewriter
    {
        private readonly ExpressionSyntax _typeExpression;
        private readonly ExpressionSyntax _tagExpression;

        public FallbackRewriter(ExpressionSyntax typeExpression, ExpressionSyntax tagExpression)
        {
            _typeExpression = typeExpression;
            _tagExpression = tagExpression;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) =>
            SyntaxFactory.InvocationExpression(node)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(_typeExpression),
                    SyntaxFactory.Argument(_tagExpression));

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            var parameters = node.ParameterList.Parameters;
            if (node.Body is InvocationExpressionSyntax invocation)
            {
                var invocationsArgs = invocation
                    .ArgumentList
                    .Arguments
                    .Select(i => i.Expression)
                    .OfType<IdentifierNameSyntax>()
                    .Select(i => i.Identifier.Text)
                    .ToArray();

                var lambdaArgs = parameters
                    .Select(i => i.Identifier.Text)
                    .ToArray();

                if (invocationsArgs.Length == 2 && lambdaArgs.Length == 2)
                {
                    var args = new[]
                    {
                        SyntaxFactory.Argument(_typeExpression),
                        SyntaxFactory.Argument(_tagExpression)
                    };

                    IEnumerable<ArgumentSyntax>? actualArgs = null;
                    if (invocationsArgs.SequenceEqual(lambdaArgs))
                    {
                        actualArgs = args;
                    }
                    else
                    {
                        if (invocationsArgs.Reverse().SequenceEqual(lambdaArgs))
                        {
                            actualArgs = args.Reverse();
                        }
                    }

                    if (actualArgs != null)
                    {
                        return SyntaxFactory.InvocationExpression(invocation.Expression)
                            .AddArgumentListArguments(actualArgs.ToArray());
                    }
                }
            }

            return base.VisitParenthesizedLambdaExpression(node);
        }
    }
}