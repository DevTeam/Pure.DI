// ReSharper disable MemberCanBePrivate.Global

namespace Pure.DI.Core;

sealed class ProcessingNode(
    DependencyNode node,
    ISet<Injection> contracts,
    IReadOnlyCollection<InjectionInfo> injections)
    : IEquatable<ProcessingNode>, IProcessingNode
{
    public DependencyNode Node => node;

    public ISet<Injection> Contracts { get; } = contracts;

    public IReadOnlyCollection<InjectionInfo> Injections => injections;

    public override string ToString() => Node.ToString();

    public override int GetHashCode() => Node.BindingId.GetHashCode();

    public override bool Equals(object? obj) => obj is ProcessingNode other && Equals(other);

    public bool Equals(ProcessingNode other) => Node.BindingId.Equals(other.Node.BindingId);
}