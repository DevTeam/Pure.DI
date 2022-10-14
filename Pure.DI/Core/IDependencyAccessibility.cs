namespace Pure.DI.Core;

internal interface IDependencyAccessibility
{
    public SyntaxKind GetSyntaxKind(SemanticType dependency);
}