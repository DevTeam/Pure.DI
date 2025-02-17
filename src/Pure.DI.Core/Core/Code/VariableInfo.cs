namespace Pure.DI.Core.Code;

sealed class VariableInfo
{
    private readonly HashSet<int> _blockParentBlocks = [];
    private readonly HashSet<int> _perBlockRefCounts = [];
    private readonly List<DependencyNode> _targetNodes = [];
    private readonly HashSet<int> _variableParentBlocks = [];
    public bool HasLocalMethod;

    public int RefCount { get; private set; } = 1;

    public int PerBlockRefCount => _perBlockRefCounts.Count + 1;

    public void AddTargetNode(DependencyNode targetNode) =>
        _targetNodes.Add(targetNode);

    public IEnumerable<DependencyNode> GetTargetNodes() =>
        _targetNodes.GroupBy(i => i.Binding.Id).Select(i => i.First());

    public void AddRef(Block parentBlock)
    {
        RefCount++;
        _perBlockRefCounts.Add(parentBlock.Id);
    }

    public bool CreateVariable(Block parentBlock) =>
        _variableParentBlocks.Add(parentBlock.Id);

    public bool CreateBlock(Block parentBlock) =>
        _blockParentBlocks.Add(parentBlock.Id);

    public void Reset()
    {
        _perBlockRefCounts.Clear();
        _variableParentBlocks.Clear();
        _blockParentBlocks.Clear();
        _targetNodes.Clear();
        RefCount = 1;
        HasLocalMethod = false;
    }
}