namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        public ResolveMethod Build()
        {
            var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax)),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

            var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxRepo.ResolversWithTagTableTypeSyntax,
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversByTagTable.Resolve))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(SyntaxRepo.FactoriesByTagTableName)),
                        SyntaxFactory.Argument(key)
                    );

            return new ResolveMethod(
                SyntaxRepo.GenericStaticResolveWithTagMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
        }
    }
}