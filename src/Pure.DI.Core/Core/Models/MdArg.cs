// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Pure.DI.Core.Models;

readonly record struct MdArg(
    SemanticModel SemanticModel,
    InvocationExpressionSyntax Source,
    ITypeSymbol Type,
    string ArgName,
    ArgKind Kind,
    bool IsBuildUpInstance,
    IReadOnlyCollection<string> Comments)
{
    public override string ToString() => $"{(Kind == ArgKind.Root ? "RootArg" : "Arg")}<{Type}>(\"{ArgName}\")";
}