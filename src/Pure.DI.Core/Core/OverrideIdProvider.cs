namespace Pure.DI.Core;

using System.Collections.Concurrent;
using static Tag;

class OverrideIdProvider([Tag(Override)] IIdGenerator idGenerator) : IOverrideIdProvider
{
    private readonly ConcurrentDictionary<Key, int> _ids = new();

    public int GetId(ITypeSymbol type, ISet<object> tags) =>
        _ids.GetOrAdd(new Key(type, tags), _ => idGenerator.Generate());

    private class Key(ITypeSymbol type, ISet<object> tags)
    {
        private readonly ITypeSymbol _type = type;
        private readonly ISet<object> _tags = tags;

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            var other = (Key)obj;
            return SymbolEqualityComparer.Default.Equals(_type, other._type) && _tags.Intersect(other._tags).Any();
        }

        public override int GetHashCode() =>
            SymbolEqualityComparer.Default.GetHashCode(_type);
    }
}