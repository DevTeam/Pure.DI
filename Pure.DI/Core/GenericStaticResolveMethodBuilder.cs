namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        public ResolveMethod Build()
        {
            var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxRepo.ResolversTableTypeSyntax,
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversTable.Resolve))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(SyntaxRepo.FactoriesTableName)),
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax))
                    );

            return new ResolveMethod(
                SyntaxRepo.GenericStaticResolveMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
        }
    }
}
