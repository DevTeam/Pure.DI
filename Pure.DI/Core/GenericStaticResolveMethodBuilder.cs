namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IMemberNameService _memberNameService;

        public GenericStaticResolveMethodBuilder(IMemberNameService memberNameService) =>
            _memberNameService = memberNameService;

        public ResolveMethod Build()
        {
            var resolve = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName(_memberNameService.GetName(MemberNameKind.FactoriesField)),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversTable.Resolve))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax))
                    );

            return new ResolveMethod(
                SyntaxRepo.GenericStaticResolveMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(SyntaxRepo.TTypeSyntax, resolve))));
        }
    }
}
