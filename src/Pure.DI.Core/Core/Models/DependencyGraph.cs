namespace Pure.DI.Core.Models;

internal record DependencyGraph(
    bool IsValid,
    in MdSetup Source,
    IGraph<DependencyNode, Dependency> Graph,
    IReadOnlyDictionary<Injection, Root> Roots);