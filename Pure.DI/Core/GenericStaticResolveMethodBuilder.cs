namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly IMemberNameService _memberNameService;
    private readonly IBuildContext _buildContext;
    private readonly ISyntaxRegistry _syntaxRegistry;
    private readonly ISettings _settings;

    public GenericStaticResolveMethodBuilder(
        IMemberNameService memberNameService,
        IBuildContext buildContext,
        ISyntaxRegistry syntaxRegistry,
        ISettings settings)
    {
        _memberNameService = memberNameService;
        _buildContext = buildContext;
        _syntaxRegistry = syntaxRegistry;
        _settings = settings;
    }

    public ResolveMethod Build()
    {
        const string resolverFieldName = "Resolve";
        var resolverKey = new MemberKey("Resolver", typeof(GenericStaticResolveMethodBuilder));
        var resolverClass = _buildContext.GetOrAddMember(resolverKey, () =>
        {
            var getResolverKey = new MemberKey("GetResolver", typeof(GenericStaticResolveMethodBuilder));
            var getResolverMethod = _buildContext.GetOrAddMember(getResolverKey, () => SyntaxRepo.GetResolverMethodSyntax.WithBody(_syntaxRegistry.FindMethod(nameof(ResolversTable), nameof(ResolversTable.GetResolver)).Body));
            return SyntaxRepo.ClassDeclaration(_memberNameService.GetName(MemberNameKind.ResolverClass))
                .AddModifiers(
                    SyntaxKind.PrivateKeyword.WithSpace(),
                    SyntaxKind.StaticKeyword.WithSpace())
                .AddTypeParameterListParameters(SyntaxRepo.TTypeParameterSyntax)
                .AddMembers(
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxRepo.FuncOfObjectTypeSyntax)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(resolverFieldName)
                                        .WithSpace()
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(SyntaxFactory.GenericName(getResolverMethod.Identifier.Text).AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax))))))
                        .AddModifiers(
                            SyntaxKind.InternalKeyword.WithSpace(),
                            SyntaxKind.StaticKeyword.WithSpace(),
                            SyntaxKind.ReadOnlyKeyword.WithSpace()));
        });

        var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.GenericName(resolverClass.Identifier).AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(nameof(ResolversTable.Resolve))))
            .AddArgumentListArguments();

        return new ResolveMethod(
            SyntaxRepo.CreateGenericStaticResolveMethodSyntax(_settings.AccessibilityToken).AddBodyStatements(
                SyntaxRepo.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
    }
}