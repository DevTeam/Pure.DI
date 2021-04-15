namespace Pure.DI.Core
{
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;

        public StaticWithTagResolveMethodBuilder(IFallbackStrategy fallbackStrategy) =>
            _fallbackStrategy = fallbackStrategy;

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var returnWithTagDefault = _fallbackStrategy.Build(semanticModel, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.ParseTypeName("type"));

            var varDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxRepo.FuncObjectTypeSyntax)
                    .AddVariables(SyntaxFactory.VariableDeclarator("factory"))
            );

            var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")),
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
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("factory")).AddArgumentListArguments())
            );

            return new ResolveMethod(
                SyntaxRepo.StaticResolveWithTagMethodSyntax.AddBodyStatements(varDeclaration, ifStatement), 
                returnWithTagDefault);
        }
    }
}