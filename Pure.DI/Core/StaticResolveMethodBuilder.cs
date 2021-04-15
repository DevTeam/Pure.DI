namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;

        public StaticResolveMethodBuilder(IFallbackStrategy fallbackStrategy) =>
            _fallbackStrategy = fallbackStrategy;

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var returnDefault = _fallbackStrategy.Build(semanticModel, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));
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
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("factory"))
                            .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                    ),
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("factory")).AddArgumentListArguments())
            );

            return new ResolveMethod(
                SyntaxRepo.StaticResolveMethodSyntax.AddBodyStatements(varDeclaration, ifStatement),
                returnDefault);
        }
    }
}