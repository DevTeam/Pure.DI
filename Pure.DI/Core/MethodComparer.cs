namespace Pure.DI.Core;

internal class MethodComparer : IEqualityComparer<MethodDeclarationSyntax>
{
    public static readonly IEqualityComparer<MethodDeclarationSyntax> Shared = new MethodComparer();

    private MethodComparer() { }

    public bool Equals(MethodDeclarationSyntax x, MethodDeclarationSyntax y) => x.Identifier.Text == y.Identifier.Text;

    public int GetHashCode(MethodDeclarationSyntax obj) => obj.Identifier.Text.GetHashCode();
}