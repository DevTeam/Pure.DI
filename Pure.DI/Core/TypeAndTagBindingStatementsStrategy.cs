namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        private readonly SemanticModel _semanticModel;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;

        public TypeAndTagBindingStatementsStrategy(SemanticModel semanticModel, IBindingExpressionStrategy bindingExpressionStrategy)
        {
            _semanticModel = semanticModel;
            _bindingExpressionStrategy = bindingExpressionStrategy;
        }

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, INamedTypeSymbol contractType, INameService nameService)
        {
            foreach (var tag in binding.Tags)
            {
                var instance = _bindingExpressionStrategy.TryBuild(binding, contractType, tag, nameService, new List<BindingMetadata>());
                yield return SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(instance)));
            }
        }
    }
}