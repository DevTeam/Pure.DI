namespace Pure.DI.Core.Models;

readonly record struct MdRoot(
    InvocationExpressionSyntax Source,
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