namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        public ResolveMethod Build()
        {
            var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

            var resolve = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName(SyntaxRepo.FactoriesByTagTableName),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversByTagTable.Resolve))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(key));

            return new ResolveMethod(
                SyntaxRepo.StaticResolveWithTagMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(resolve)));
        }
    }
}