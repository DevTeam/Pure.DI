namespace Pure.DI.Core.Models;

using CSharp;

internal readonly record struct Injection(
    ITypeSymbol Type,
    object? Tag)
{
    public override string ToString() => $"{Type}{(Tag != default ? $"({Tag.ValueToString()})" : "")}";

    public bool Equals(Injection other) => 
        SymbolEqualityComparer.Default.Equals(Type, other.Type) && EqualTags(Tag, other.Tag);

    public override int GetHashCode() => 
        SymbolEqualityComparer.Default.GetHashCode(Type);

    private static bool EqualTags(object? tag, object? otherTag)
    {
        if (ReferenceEquals(tag, MdTag.ContextTag))
        {
            return true;
        }
        
        return ReferenceEquals(otherTag, MdTag.ContextTag) || Equals(tag, otherTag);
    }
}