namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITargetClassNameProvider
    {
        string? TryGetName(string composerTypeName, SyntaxNode node, ClassDeclarationSyntax? ownerClass);
    }
}
