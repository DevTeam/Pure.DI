namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        public IEnumerable<StatementSyntax> CreateStatements(
            IBindingExpressionStrategy bindingExpressionStrategy,
            BindingMetadata binding,
            ITypeSymbol contractType)
        {
            foreach (var tag in binding.Tags)
            {
                var instance = bindingExpressionStrategy.TryBuild(contractType, tag);
                yield return SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(instance)));
            }
        }
    }
}