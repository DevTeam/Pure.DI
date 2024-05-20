namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    private readonly HashSet<int> _perBlockRefCounts = [];
    private readonly HashSet<Block> _parentBlocks = [];
    public bool HasLocalMethod;
    
    public int RefCount { get; private set; } = 1;

    public int PerBlockRefCount => _perBlockRefCounts.Count + 1;

    public void AddRef(Block parentBlock)
    {
        RefCount++;
        _perBlockRefCounts.Add(parentBlock.Id);
    }

    public bool Create(Block parentBlock) =>
        _parentBlocks.Add(parentBlock);

    public void Reset()
    {
        _perBlockRefCounts.Clear();
        _parentBlocks.Clear();
        RefCount = 1;
        HasLocalMethod = false;
    }
}