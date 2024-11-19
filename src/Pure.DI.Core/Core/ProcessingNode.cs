// ReSharper disable MemberCanBePrivate.Global

namespace Pure.DI.Core;

internal class ProcessingNode : IEquatable<ProcessingNode>
{
    public readonly DependencyNode Node;
    private readonly Lazy<ImmutableArray<InjectionInfo>> _injections;

    public ProcessingNode(
        DependencyNode node,
        ISet<Injection> contracts)
    {
        Node = node;
        Contracts = contracts;
        _injections = new Lazy<ImmutableArray<InjectionInfo>>(GetInjections);
        return;

        ImmutableArray<InjectionInfo> GetInjections()
        {
            var injectionsWalker = new DependenciesToInjectionsWalker();
            injectionsWalker.VisitDependencyNode(Unit.Shared, node);
            return injectionsWalker.ToImmutableArray();
        }
    }

    public ISet<Injection> Contracts { get; }

    public ImmutableArray<InjectionInfo> Injections => _injections.Value;

    public override string ToString() => Node.ToString();

    public bool Equals(ProcessingNode other) => Node.Equals(other.Node);

    public override bool Equals(object? obj) => obj is ProcessingNode other && Equals(other);

    public override int GetHashCode() => Node.GetHashCode();
}