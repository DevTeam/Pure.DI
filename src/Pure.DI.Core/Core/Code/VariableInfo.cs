namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    private readonly HashSet<int> _perBlockRefCounts = [];
    private readonly HashSet<int> _variableParentBlocks = [];
    private readonly HashSet<int> _blockParentBlocks = [];
    public bool HasLocalMethod;
    
    public int RefCount { get; private set; } = 1;

    public int PerBlockRefCount => _perBlockRefCounts.Count + 1;

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
        RefCount = 1;
        HasLocalMethod = false;
    }
}