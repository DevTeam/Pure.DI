namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class OwnerProvider : IOwnerProvider
    {
        public ClassDeclarationSyntax? TryGetOwner(SyntaxNode node) => (
                from cls in node.Ancestors().OfType<ClassDeclarationSyntax>()
                where
                    cls.Modifiers.Any(i => i.Kind() == SyntaxKind.StaticKeyword)
                    && cls.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword)
                    && cls.Modifiers.All(i => i.Kind() != SyntaxKind.PrivateKeyword)
                select cls)
            .FirstOrDefault();
    }
}