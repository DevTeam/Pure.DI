namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;

        public GenericStaticWithTagResolveMethodBuilder(IFallbackStrategy fallbackStrategy) =>
            _fallbackStrategy = fallbackStrategy;

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var genericWithTagReturnDefault = _fallbackStrategy.Build(semanticModel, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.ParseTypeName("tag"));

            var varDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxRepo.FuncObjectTypeSyntax)
                    .AddVariables(SyntaxFactory.VariableDeclarator("factory"))
            );

            var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax)),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));

            var ifStatement = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName(SyntaxRepo.ResolversWithTagTableName),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversWithTagTable.TryGet))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(key),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("factory"))
                            .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                    ),
                
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.CastExpression(
                        SyntaxRepo.TTypeSyntax, 
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName("factory")).AddArgumentListArguments()))
            );
            return new ResolveMethod(
                SyntaxRepo.GenericStaticResolveWithTagMethodSyntax.AddBodyStatements(varDeclaration, ifStatement), 
                genericWithTagReturnDefault);
        }
    }
}