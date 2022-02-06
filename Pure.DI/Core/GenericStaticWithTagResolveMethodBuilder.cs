namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly IMemberNameService _memberNameService;

    public GenericStaticWithTagResolveMethodBuilder(IMemberNameService memberNameService) =>
        _memberNameService = memberNameService;

    public ResolveMethod Build()
    {
        var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax)),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

        var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseName(_memberNameService.GetName(MemberNameKind.FactoriesByTagField)),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(nameof(ResolversByTagTable.Resolve))))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(key)
            );

        return new ResolveMethod(
            SyntaxRepo.GenericStaticResolveWithTagMethodSyntax.AddBodyStatements(
                SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
    }
}