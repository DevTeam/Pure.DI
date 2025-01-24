namespace Pure.DI.Core.Models;

internal readonly record struct MdRoot(
    SyntaxNode Source,
    SemanticModel SemanticModel,
    ITypeSymbol RootType,
    string Name,
    MdTag? Tag,
    RootKinds Kind,
    IReadOnlyCollection<string> Comments,
    bool IsBuilder)
{
    public override string ToString() => $"Root<{RootType}>(\"{Name}\", {Tag.ToString()})";
}