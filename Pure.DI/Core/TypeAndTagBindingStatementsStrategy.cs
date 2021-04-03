namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;
        private readonly ISet<BindingMetadata> _additionalBindings;

        public TypeAndTagBindingStatementsStrategy(IBindingExpressionStrategy bindingExpressionStrategy, ISet<BindingMetadata> additionalBindings)
        {
            _bindingExpressionStrategy = bindingExpressionStrategy;
            _additionalBindings = additionalBindings;
        }

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, INamedTypeSymbol contractType)
        {
            foreach (var tag in binding.Tags)
            {
                var instance = _bindingExpressionStrategy.TryBuild(binding, contractType, tag, _additionalBindings);
                yield return SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(instance)));
            }
        }
    }
}