namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    private readonly HashSet<int> _perBlockRefCounts = [];
    public readonly HashSet<Block> Owners = [];
    public bool IsCreated;
    public bool HasCode;
    public LinesBuilder Code = new();
    
    public int RefCount { get; private set; } = 1;

    public int PerBlockRefCount => _perBlockRefCounts.Count;

    public void AddRef(Block parentBlock)
    {
        RefCount++;
        _perBlockRefCounts.Add(parentBlock.Id);
    }

    public void Reset()
    {
        _perBlockRefCounts.Clear();
        Owners.Clear();
        RefCount = 1;
        IsCreated = false;
        HasCode = false;
        Code = new LinesBuilder();
    }
}