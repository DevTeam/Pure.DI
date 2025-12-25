namespace Pure.DI.Core.Models;

record MdRoot(
    int OriginalId,
    InvocationExpressionSyntax Source,
    SemanticModel SemanticModel,
    ITypeSymbol RootType,
    string Name,
    MdTag? Tag,
    RootKinds Kind,
    IReadOnlyCollection<string> Comments,
    ITypeSymbol RootContractType,
    bool IsBuilder,
    ImmutableArray<MdRoot> BuilderRoots = default)
{
    public override string ToString() => $"Root<{RootType}>(\"{Name}\", {Tag.ToString()})";
}