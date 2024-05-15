namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    private readonly HashSet<int> _perBlockRefCounts = [];
    public readonly HashSet<Block> Owners = [];
    public readonly HashSet<Block> Created = [];
    public bool HasLocalMethod;
    
    public int RefCount { get; private set; } = 1;

    public int PerBlockRefCount => _perBlockRefCounts.Count + 1;

    public void AddRef(Block parentBlock)
    {
        RefCount++;
        _perBlockRefCounts.Add(parentBlock.Id);
    }
    
    public bool MarkAsCreated(Block block)
    {
        return Created.Add(block);
    }

    public bool IsCreated(Block block)
    {
        return Created.Contains(block);
    }

    public void Reset()
    {
        _perBlockRefCounts.Clear();
        Owners.Clear();
        Created.Clear();
        RefCount = 1;
        HasLocalMethod = false;
    }
}