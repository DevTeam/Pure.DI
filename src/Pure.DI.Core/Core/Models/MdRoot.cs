namespace Pure.DI.Core.Models;

record MdRoot(
    int OriginalId,
    ExpressionSyntax Source,
    SemanticModel SemanticModel,
    ITypeSymbol RootType,
    string Name,
    string UniqueName,
    MdTag? Tag,
    RootKinds Kind,
    IReadOnlyCollection<string> Comments,
    ITypeSymbol RootContractType,
    bool IsBuilder,
    ImmutableArray<MdRoot> BuilderRoots = default,
    LightweightKind LightweightKind = LightweightKind.None)
{
    public override string ToString() => $"Root<{RootType}>(\"{Name}\", {Tag.ToString()})";
}