namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IOwnerProvider
    {
        ClassDeclarationSyntax? TryGetOwner(SyntaxNode node);
    }
}
