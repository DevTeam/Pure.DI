// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdTag(
    int Position,
    object? Value)
{
    public static readonly object ContextTag = new ContextTagObject();
    public static readonly object AnyTag = new AnyTagObject();

    public bool Equals(MdTag other) => Equals(Value, other.Value);

    public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

    public static MdTag CreateTypeTag(MdTag baseTag, ITypeSymbol? type) =>
        baseTag with { Value = type };

    public static MdTag CreateUniqueTag(MdTag baseTag, int id) =>
        baseTag with { Value = CreateUniqueTagValue(id) };

    public static MdTag CreateAnyTag(MdTag baseTag) =>
        baseTag with { Value = AnyTag };

    private static object CreateUniqueTagValue(int id) =>
        new UniqueTag(id);

    public static object CreateTagOnValue(SyntaxNode source, in ImmutableArray<MdInjectionSite> injectionSites) =>
        new MdTagOnSites(source, injectionSites);

    public override string ToString() =>
        Value switch
        {
            null => "null",
            string => $"\"{Value}\"",
            MdTagOnSites => "",
            _ => Value.ToString()
        };

    private class ContextTagObject
    {
        public override string ToString() => "\"ContextTag\"";
    }

    private class AnyTagObject
    {
        public override string ToString() => $"{Names.ApiNamespace}{nameof(Tag)}.{nameof(Tag.Any)}";
    }
}