namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    public readonly HashSet<Block> Owners = new();
    public int RefCount = 1;
    
    public bool IsCreated { get; set; }
    
    public void Reset()
    {
        Owners.Clear();
        RefCount = 1;
        IsCreated = false;
    }
}