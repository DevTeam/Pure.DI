namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    public readonly HashSet<Block> Owners = [];
    public bool IsCreated;
    public bool HasCode;
    public LinesBuilder Code = new();
    
    public int RefCount { get; private set; } = 1;

    public void AddRef()
    {
        RefCount++;
    }

    public void Reset()
    {
        Owners.Clear();
        RefCount = 1;
        IsCreated = false;
        HasCode = false;
        Code = new LinesBuilder();
    }
}