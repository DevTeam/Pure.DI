namespace Pure.DI.Core;

using System.Collections.Concurrent;
using static Tag;

class OverrideIdProvider([Tag(OverridesIdGenerator)] IIdGenerator idGenerator) : IOverrideIdProvider
{
    private readonly ConcurrentDictionary<Key, int> _ids = new();

    public int GetId(ITypeSymbol type, in ImmutableArray<MdTag> tags) =>
        _ids.GetOrAdd(new Key(type, tags), _ => idGenerator.Generate());

    private class Key(ITypeSymbol type, in ImmutableArray<MdTag> tags)
    {
        private static readonly object NullTag = new();
        private readonly ITypeSymbol _type = type;
        private readonly ISet<object> _tags = tags.Select(i => i.Value ?? NullTag).ToImmutableHashSet();

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            var other = (Key)obj;
            return SymbolEqualityComparer.Default.Equals(_type, other._type)
                   && _tags.SetEquals(other._tags);
        }

        public override int GetHashCode()
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(_type);
            foreach (var tagHashCode in _tags.Select(GetTagHashCode).OrderBy(i => i))
            {
                hashCode = (hashCode * 397) ^ tagHashCode;
            }

            return hashCode;
        }

        private static int GetTagHashCode(object tag) =>
            tag.GetHashCode();
    }
}
