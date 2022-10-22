// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal sealed class OwnerProvider : IOwnerProvider
{
    public ClassDeclarationSyntax? TryGetOwner(SyntaxNode node)
    {
        var classes = node.Ancestors().OfType<ClassDeclarationSyntax>().ToArray();
        if (classes.All(
                cls =>
                    cls.Modifiers.Any(i => i.IsKind(SyntaxKind.PartialKeyword))
                    && cls.Modifiers.Any(i => !i.IsKind(SyntaxKind.PrivateKeyword) && !i.IsKind(SyntaxKind.ProtectedKeyword))))
        {
            var cls = classes.FirstOrDefault();
            if (cls != null && cls.Modifiers.Any(i => i.IsKind(SyntaxKind.StaticKeyword)))
            {
                return cls;
            }
        }

        return null;
    }
}