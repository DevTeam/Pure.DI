namespace Pure.DI.Core.Models;

readonly record struct Injection(
    InjectionKind Kind,
    RefKind RefKind,
    ITypeSymbol Type,
    object? Tag,
    ImmutableArray<Location> Locations)
{
    public override string ToString() => $"{Type}{(Tag != null && Tag is not MdTagOnSites ? $"({Tag.ValueToString()})" : "")}";

    public bool Equals(Injection other) =>
        (ReferenceEquals(Type, other.Type) || SymbolEqualityComparer.Default.Equals(Type, other.Type))
        && EqualTags(Tag, other.Tag);

    public override int GetHashCode() =>
        SymbolEqualityComparer.Default.GetHashCode(Type);

    public static bool EqualTags(object? tag, object? otherTag) =>
        ReferenceEquals(tag, otherTag)
        || SpecialEqualTags(tag, otherTag)
        || SpecialEqualTags(otherTag, tag)
        || Equals(tag, otherTag);

    private static bool SpecialEqualTags(object? tag, object? otherTag) =>
        ReferenceEquals(tag, MdTag.ContextTag)
        || ReferenceEquals(tag, MdTag.AnyTag)
        || tag is MdTagOnSites tagOn && tagOn.Equals(otherTag);
}