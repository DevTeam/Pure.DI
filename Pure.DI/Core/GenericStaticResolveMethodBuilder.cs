namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;

        public GenericStaticResolveMethodBuilder(IFallbackStrategy fallbackStrategy) =>
            _fallbackStrategy = fallbackStrategy;

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var genericReturnDefault = _fallbackStrategy.Build(semanticModel, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));

            var varDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxRepo.FuncObjectTypeSyntax)
                    .AddVariables(SyntaxFactory.VariableDeclarator("factory"))
            );

            var ifStatement = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName(SyntaxRepo.ResolversTableName),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(nameof(ResolversTable.TryGet))))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax)),
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
                SyntaxRepo.GenericStaticResolveMethodSyntax.AddBodyStatements(varDeclaration, ifStatement),
                genericReturnDefault);
        }
    }
}
