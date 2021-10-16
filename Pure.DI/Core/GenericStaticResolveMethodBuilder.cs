namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IMemberNameService _memberNameService;
        private readonly IBuildContext _buildContext;
        private readonly ISyntaxRegistry _syntaxRegistry;

        public GenericStaticResolveMethodBuilder(
            IMemberNameService memberNameService,
            IBuildContext buildContext,
            ISyntaxRegistry syntaxRegistry)
        {
            _memberNameService = memberNameService;
            _buildContext = buildContext;
            _syntaxRegistry = syntaxRegistry;
        }

        public ResolveMethod Build()
        {
            const string resolverFieldName = "Resolve";
            var resolverKey = new MemberKey("Resolver", typeof(GenericStaticResolveMethodBuilder));
            var resolverClass = _buildContext.GetOrAddMember(resolverKey, () =>
            {
                var getResolverKey = new MemberKey("GetResolver", typeof(GenericStaticResolveMethodBuilder));
                var getResolverMethod = _buildContext.GetOrAddMember(getResolverKey, () => SyntaxRepo.GetResolverMethodSyntax.WithBody(_syntaxRegistry.FindMethod(nameof(ResolversTable), nameof(ResolversTable.GetResolver)).Body));
                return SyntaxFactory.ClassDeclaration(_memberNameService.GetName(MemberNameKind.ResolverClass))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .AddTypeParameterListParameters(SyntaxRepo.TTypeParameterSyntax)
                    .AddMembers(
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(SyntaxRepo.FuncOfObjectTypeSyntax)
                                    .AddVariables(
                                        SyntaxFactory.VariableDeclarator(resolverFieldName)
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(SyntaxFactory.GenericName(getResolverMethod.Identifier.Text).AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax))))))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
            });

            var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.GenericName(resolverClass.Identifier).AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversTable.Resolve))))
                    .AddArgumentListArguments();

            return new ResolveMethod(
                SyntaxRepo.GenericStaticResolveMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
        }
    }
}
