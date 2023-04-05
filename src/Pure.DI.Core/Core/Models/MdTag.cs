// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdTag(
    int Position,
    object Value)
{
    public static readonly object ContextTag = new ContextTagObject();
    
    public override string ToString() =>
        Value switch
        {
            not {} => "null",
            string => $"\"{Value}\"",
            _ => Value.ToString()
        };
    
    private class ContextTagObject
    {
        public override string ToString() => "ContextTag";
    }
}