namespace Pure.DI.Core.Models;

readonly record struct Dependency(
    bool IsResolved,
    DependencyNode Source,
    in Injection Injection,
    DependencyNode Target)
    : IEdge<DependencyNode>
{
    public override string ToString() => $"[{Target}]<--[{Injection.ToString()}]--[{Source}]";
}