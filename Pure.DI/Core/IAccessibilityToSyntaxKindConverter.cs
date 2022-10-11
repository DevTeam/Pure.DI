namespace Pure.DI.Core;

internal interface IAccessibilityToSyntaxKindConverter
{
    SyntaxKind Convert(Accessibility accessibility);
}