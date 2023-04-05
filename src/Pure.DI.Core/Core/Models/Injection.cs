namespace Pure.DI.Core.Models;

internal readonly record struct Injection(
    ITypeSymbol Type,
    object? Tag)
{
    public override string ToString() => $"{Type}{(Tag != default ? $"({Tag})" : "")}";

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
        
        if (ReferenceEquals(otherTag, MdTag.ContextTag))
        {
            return true;
        }
        
        if (Equals(tag, otherTag))
        {
            return true;
        }

        if (tag is CompositeTag compositeTag)
        {
            return EqualCompositeTags(otherTag, compositeTag);
        }
        
        if (otherTag is CompositeTag otherCompositeTag)
        {
            return EqualCompositeTags(tag, otherCompositeTag);
        }

        return tag is CompositeTag compositeTag1
               && otherTag is CompositeTag compositeTag2
               && compositeTag1.Tags.Intersect(compositeTag2.Tags).Any();
    }

    private static bool EqualCompositeTags(object? tag, CompositeTag compositeTag) => 
        tag is not null
            ? compositeTag.Tags.Contains(tag)
            : compositeTag.Tags.IsEmpty;
}