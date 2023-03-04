namespace Pure.DI.Core.Models;

internal record DependencyGraph(
    in MdSetup Source,
    IGraph<DependencyNode, Dependency> Graph,
    IReadOnlyDictionary<Injection, DependencyNode> Map,
    IReadOnlyDictionary<Injection, Root> Roots);