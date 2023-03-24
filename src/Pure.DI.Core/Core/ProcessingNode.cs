namespace Pure.DI.Core;

internal readonly struct ProcessingNode
{
    public readonly bool HasNode = false;
    public readonly DependencyNode Node;
    private readonly IMarker _marker;
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private readonly Lazy<bool> _isMarkerBased;
    private readonly Lazy<ImmutableArray<Injection>> _exposedInjections;
    private readonly Lazy<ImmutableArray<Injection>> _injections;

    public ProcessingNode(
        DependencyNode node,
        IMarker marker,
        IBuilder<MdBinding, ISet<Injection>> injectionsBuilder)
    {
        HasNode = true;
        Node = node;
        _marker = marker;
        _injectionsBuilder = injectionsBuilder;
        _isMarkerBased = new Lazy<bool>(() => marker.IsMarkerBased(node.Type));
        _exposedInjections = new Lazy<ImmutableArray<Injection>>(() => injectionsBuilder.Build(node.Binding, CancellationToken.None).ToImmutableArray());
        _injections = new Lazy<ImmutableArray<Injection>>(() =>
        {
            var injectionsWalker = new DependenciesToInjectionsWalker();
            injectionsWalker.VisitDependencyNode(node);
            return injectionsWalker.ToImmutableArray();    
        });
    }

    public bool IsMarkerBased => _isMarkerBased.Value;
        
    public ImmutableArray<Injection> ExposedInjections => _exposedInjections.Value;

    public ImmutableArray<Injection> Injections => _injections.Value;

    public override string ToString() => Node.ToString();

    public ProcessingNode CreateNew(DependencyNode newNode) => new(newNode, _marker, _injectionsBuilder);
}