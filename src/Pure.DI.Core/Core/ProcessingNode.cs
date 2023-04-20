// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.Core;

internal readonly struct ProcessingNode
{
    public readonly bool HasNode = false;
    public readonly DependencyNode Node;
    private readonly Lazy<bool> _isMarkerBased;
    private readonly Lazy<ImmutableArray<Injection>> _injections;

    public ProcessingNode(
        DependencyNode node,
        ISet<Injection> contracts,
        IMarker marker)
    {
        HasNode = true;
        Node = node;
        Contracts = contracts;

        bool IsMarkerBased() => marker.IsMarkerBased(node.Type);
        _isMarkerBased = new Lazy<bool>(IsMarkerBased);

        ImmutableArray<Injection> GetInjections()
        {
            var injectionsWalker = new DependenciesToInjectionsWalker();
            injectionsWalker.VisitDependencyNode(node);
            return injectionsWalker.ToImmutableArray();
        }
        _injections = new Lazy<ImmutableArray<Injection>>(GetInjections);
    }

    public bool IsMarkerBased => _isMarkerBased.Value;
        
    public ISet<Injection> Contracts { get; }

    public ImmutableArray<Injection> Injections => _injections.Value;

    public override string ToString() => Node.ToString();

    public bool Equals(ProcessingNode other) => Node.Equals(other.Node);

    public override bool Equals(object? obj) => obj is ProcessingNode other && Equals(other);

    public override int GetHashCode() => Node.GetHashCode();
}