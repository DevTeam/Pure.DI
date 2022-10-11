// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class AccessibilityToSyntaxKindConverter : IAccessibilityToSyntaxKindConverter
{
    public SyntaxKind Convert(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => SyntaxKind.PublicKeyword,
            Accessibility.Private => SyntaxKind.PrivateKeyword,
            Accessibility.Protected => SyntaxKind.InternalKeyword,
            Accessibility.Internal => SyntaxKind.InternalKeyword,
            Accessibility.ProtectedAndInternal => SyntaxKind.InternalKeyword,
            Accessibility.ProtectedOrInternal => SyntaxKind.InternalKeyword,
            _ => SyntaxKind.PublicKeyword
        };
}