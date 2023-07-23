// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI;

public class Unit
{
    public static readonly Unit Shared = new();
    
    private Unit() { }

    public override string ToString() => string.Empty;
}