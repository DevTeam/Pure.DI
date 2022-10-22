namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly IMemberNameService _memberNameService;
    private readonly ISettings _settings;

    public GenericStaticWithTagResolveMethodBuilder(IMemberNameService memberNameService, ISettings settings)
    {
        _memberNameService = memberNameService;
        _settings = settings;
    }

    public ResolveMethod Build()
    {
        var tagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).FullName.ReplaceNamespace());
        var key = SyntaxRepo.ObjectCreationExpression(tagTypeTypeSyntax)
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
            SyntaxRepo.CreateGenericStaticResolveWithTagMethodSyntax(_settings.AccessibilityToken).AddBodyStatements(
                SyntaxRepo.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
    }
}