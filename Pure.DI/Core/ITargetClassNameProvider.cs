namespace Pure.DI.Core;

internal interface ITargetClassNameProvider
{
    string? TryGetName(string composerTypeName, SyntaxNode node, ClassDeclarationSyntax? ownerClass);
}