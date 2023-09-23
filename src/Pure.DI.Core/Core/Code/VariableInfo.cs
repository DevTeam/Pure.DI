namespace Pure.DI.Core.Code;

internal class VariableInfo
{
    public int Level = int.MaxValue;
    public int RefCount;
    public bool IsAlreadyCreated;
    
    public void Reset()
    {
        Level = int.MaxValue;
        RefCount = 0;
        IsAlreadyCreated = false;
    }
}