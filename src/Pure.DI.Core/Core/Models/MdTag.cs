// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdTag(
    int Position,
    object? Value)
{
    public static readonly object ContextTag = new ContextTagObject();
    
    public static MdTag CreateTypeTag(MdTag baseTag, ITypeSymbol? type) =>
        baseTag with { Value = type };

    public static MdTag CreateUniqueTag(MdTag baseTag, int id) =>
        baseTag with { Value = CreateUniqueTagValue(id) };

    private static object CreateUniqueTagValue(int id) => 
        new UniqueTag(id);

    public override string ToString() =>
        Value switch
        {
            null => "null",
            string => $"\"{Value}\"",
            _ => Value.ToString()
        };
    
    private class ContextTagObject
    {
        public override string ToString() => "ContextTag";
    }
}