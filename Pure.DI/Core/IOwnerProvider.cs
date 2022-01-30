namespace Pure.DI.Core;

internal interface IOwnerProvider
{
    ClassDeclarationSyntax? TryGetOwner(SyntaxNode node);
}