namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        private readonly ITypeResolver _typeResolver;

        public TypeAndTagBindingStatementsStrategy(ITypeResolver typeResolver) =>
            _typeResolver = typeResolver;

        public IEnumerable<StatementSyntax> CreateStatements(
            IBuildStrategy buildStrategy,
            BindingMetadata binding,
            ITypeSymbol contractType)
        {
            foreach (var tag in binding.Tags)
            {
                var instanceTypeDescriptor = _typeResolver.Resolve(contractType, tag);
                var instance = buildStrategy.Build(instanceTypeDescriptor);
                yield return SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(instance)));
            }
        }
    }
}