// ReSharper disable MemberCanBePrivate.Global

namespace Pure.DI.Core;

sealed class ProcessingNode(
    IInjectionsWalker injectionsWalker,
    DependencyNode node,
    ISet<Injection> contracts)
    : IEquatable<ProcessingNode>, IProcessingNode
{
    private readonly Lazy<IReadOnlyCollection<InjectionInfo>> _injections = new(() => GetInjections(injectionsWalker, node));

    public bool Equals(ProcessingNode other) => Node.Equals(other.Node);

    public DependencyNode Node => node;

    public ISet<Injection> Contracts { get; } = contracts;

    public IReadOnlyCollection<InjectionInfo> Injections => _injections.Value;

    public override string ToString() => Node.ToString();

    public override bool Equals(object? obj) => obj is ProcessingNode other && Equals(other);

    public override int GetHashCode() => Node.GetHashCode();

    private static IReadOnlyCollection<InjectionInfo> GetInjections(IInjectionsWalker injectionsWalker, DependencyNode node)
    {
        injectionsWalker.VisitDependencyNode(Unit.Shared, node);
        return injectionsWalker.GetResult();
    }
}