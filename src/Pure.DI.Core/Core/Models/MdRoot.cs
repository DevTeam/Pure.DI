namespace Pure.DI.Core.Models;

internal readonly record struct MdRoot(
    SyntaxNode Source,
    SemanticModel SemanticModel,
    ITypeSymbol RootType,
    string Name,
    MdTag? Tag)
{
    public override string ToString() => $"Root<{RootType}>(\"{Name}\", {Tag.ToString()})";   
}