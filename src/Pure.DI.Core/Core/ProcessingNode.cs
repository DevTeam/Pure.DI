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
        _isMarkerBased = new Lazy<bool>(() => marker.IsMarkerBased(node.Type));
        _injections = new Lazy<ImmutableArray<Injection>>(() =>
        {
            var injectionsWalker = new DependenciesToInjectionsWalker();
            injectionsWalker.VisitDependencyNode(node);
            return injectionsWalker.ToImmutableArray();    
        });
    }

    public bool IsMarkerBased => _isMarkerBased.Value;
        
    public ISet<Injection> Contracts { get; }

    public ImmutableArray<Injection> Injections => _injections.Value;

    public override string ToString() => Node.ToString();
}