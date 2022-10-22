// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class SharedContextMembersBuilder: IMembersBuilder
{
    private readonly IMemberNameService _memberNameService;

    public SharedContextMembersBuilder(IMemberNameService memberNameService) =>
        _memberNameService = memberNameService;

    public int Order => 0;
    
    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var contextTypeSyntax = SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass));
        yield return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(contextTypeSyntax)
                    .AddVariables(
                        SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.ContextField))
                            .WithSpace()
                            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxRepo.ObjectCreationExpression(contextTypeSyntax).AddArgumentListArguments()))))
            .AddModifiers(
                SyntaxKind.PrivateKeyword.WithSpace(),
                SyntaxKind.StaticKeyword.WithSpace(),
                SyntaxKind.ReadOnlyKeyword.WithSpace())
            .WithCommentBefore("// Shared context to use in factories")
            .WithNewLine()
            .WithNewLine();
    }
}