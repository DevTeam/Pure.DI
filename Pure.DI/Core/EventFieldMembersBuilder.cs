// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal sealed class EventFieldMembersBuilder: IMembersBuilder
{
    public int Order => 0;
    
    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var registerDisposableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(RegisterDisposable).FullName.ReplaceNamespace());
        yield return SyntaxRepo.EventFieldDeclaration(
                SyntaxFactory.VariableDeclaration(registerDisposableTypeSyntax).AddVariables(
                    SyntaxFactory.VariableDeclarator(SyntaxRepo.OnDisposableEventName)
                        .WithSpace()
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.AnonymousMethodExpression()
                                    .WithDelegateKeyword(SyntaxKind.DelegateKeyword.WithSpace())
                                    .WithBlock(SyntaxFactory.Block())))))
            .AddModifiers(
                SyntaxKind.InternalKeyword.WithSpace(),
                SyntaxKind.StaticKeyword.WithSpace())
            .WithNewLine()
            .WithNewLine();
    }
}