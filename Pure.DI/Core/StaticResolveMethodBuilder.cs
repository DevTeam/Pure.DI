namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticResolveMethodBuilder : IResolveMethodBuilder
    {
        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName(SyntaxRepo.ResolversTableName),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(DependencyTable.Resolve))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")));

            return new ResolveMethod(
                SyntaxRepo.StaticResolveMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(resolve)));
        }
    }
}