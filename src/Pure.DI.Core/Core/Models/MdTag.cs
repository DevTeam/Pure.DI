// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdTag(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    int Position,
    object Value)
{
    public override string ToString() =>
        Value switch
        {
            not {} => "null",
            string => $"\"{Value}\"",
            _ => Value.ToString()
        };
}