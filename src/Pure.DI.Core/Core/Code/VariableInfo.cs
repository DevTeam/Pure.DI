namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    public int Level = int.MaxValue;
    public int RefCount = 1;
    
    public bool IsCreated { get; set; }
    
    public void Reset()
    {
        Level = int.MaxValue;
        RefCount = 1;
        IsCreated = false;
    }
}