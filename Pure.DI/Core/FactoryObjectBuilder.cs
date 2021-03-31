namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FactoryObjectBuilder: IObjectBuilder
    {
        public ExpressionSyntax TryBuild(
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax tag,
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            int level = 0)
        {
            var factory = binding.Factory;
            if (factory.ExpressionBody != null)
            {
                return (ExpressionSyntax)new RemoveContextRewriter(factory.Parameter.Identifier).Visit(factory.ExpressionBody);
            }

            if (factory.Block != null)
            {
                var funcName = SyntaxFactory.GenericName(nameof(Func<object>))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList()
                            .AddArguments(ResolverBuilder.ContextTypeSyntax)
                            .AddArguments(contractType.ToTypeSyntax(semanticModel)));

                var createFunc = SyntaxFactory.ObjectCreationExpression(funcName)
                    .AddArgumentListArguments(SyntaxFactory.Argument(binding.Factory));

                return SyntaxFactory.InvocationExpression(createFunc)
                    .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ResolverBuilder.SharedContextName)));
            }

            return factory;
        }

        private class RemoveContextRewriter: CSharpSyntaxRewriter
        {
            private readonly SyntaxToken _contextIdentifier;

            public RemoveContextRewriter(SyntaxToken contextIdentifier) =>
                _contextIdentifier = contextIdentifier;

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (
                    node.Expression is MemberAccessExpressionSyntax memberAccess
                    && memberAccess.Expression is IdentifierNameSyntax identifierName
                    && identifierName.ToString() == _contextIdentifier.Text)
                {
                    var name = memberAccess.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();
                    if (name != null)
                    {
                        return SyntaxFactory.InvocationExpression(name, node.ArgumentList);
                    }
                }

                return base.VisitInvocationExpression(node);
            }
        }
    }
}
