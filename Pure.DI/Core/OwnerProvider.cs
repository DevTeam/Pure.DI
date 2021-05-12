// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class OwnerProvider : IOwnerProvider
    {
        public ClassDeclarationSyntax? TryGetOwner(SyntaxNode node)
        {
            var classes = node.Ancestors().OfType<ClassDeclarationSyntax>().ToArray();
            if (classes.All(
                    cls => 
                        cls.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword)
                        && cls.Modifiers.Any(i => i.Kind() != SyntaxKind.PrivateKeyword && i.Kind() != SyntaxKind.ProtectedKeyword)))
            {
                var cls = classes.FirstOrDefault();
                if (cls != null && cls.Modifiers.Any(i => i.Kind() == SyntaxKind.StaticKeyword))
                {
                    return cls;
                }
            }

            return null;
        }
    }
}