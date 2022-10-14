// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class DisposeMembersBuilder: IMembersBuilder
{
    private readonly IBuildContext _buildContext;

    public DisposeMembersBuilder(IBuildContext buildContext) =>
        _buildContext = buildContext;

    public int Order => 0;
    
    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var registerDisposableEventTypeSyntax = SyntaxFactory.ParseTypeName(typeof(RegisterDisposableEvent).FullName.ReplaceNamespace());
        yield return SyntaxRepo.FinalDisposeMethodSyntax.AddBodyStatements(_buildContext.FinalDisposeStatements.ToArray()).WithNewLine();
        yield return SyntaxRepo.RaiseOnDisposableMethodSyntax.AddBodyStatements(
            SyntaxRepo.ExpressionStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SyntaxRepo.OnDisposableEventName))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(
                            SyntaxRepo.ObjectCreationExpression(registerDisposableEventTypeSyntax)
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.CastExpression(SyntaxRepo.DisposableTypeSyntax, SyntaxFactory.IdentifierName("disposable"))),
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("lifetime")))))),
            SyntaxRepo.ReturnStatement(SyntaxFactory.IdentifierName("disposable"))
        ).WithNewLine().WithNewLine();
    }
}