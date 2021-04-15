// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable ArrangeDefaultValueWhenTypeNotEvident
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable MergeConditionalExpression
// ReSharper disable UnusedMember.Global
namespace Pure.DI.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal class ResolversTable: Table<Type, Func<object>>
    {
        public ResolversTable(ICollection<KeyValuePair<Type, Func<object>>> pairs) : base(pairs) { }
    }

    internal class ResolversWithTagTable : Table<TagKey, Func<object>>
    {
        public ResolversWithTagTable(ICollection<KeyValuePair<TagKey, Func<object>>> pairs) : base(pairs) { }
    }

    internal struct TagKey
    {
        private readonly Type _type;
        private readonly object _tag;
        private readonly int _hashCode;

        public TagKey(Type type, object tag)
        {
            _type = type;
            _tag = tag;
            unchecked
            {
                _hashCode = (type.GetHashCode() * 397) ^ (tag != null ? tag.GetHashCode() : 0);
            }
        }

        public override bool Equals(object obj) =>
            obj is TagKey other
            && _type == other._type
            && Equals(_tag, other._tag);

        public override int GetHashCode() => _hashCode;
    }

    internal class Table<TKey, TValue>
    {
        private uint _divisor;
        private KeyValuePair<TKey, TValue>[][] _buckets;

        public Table(ICollection<KeyValuePair<TKey, TValue>> pairs)
        {
            _divisor = (uint)pairs.Count + 1;
            _buckets = new KeyValuePair<TKey, TValue>[_divisor][];

#pragma warning disable CA1806
            (
                from number in Enumerable.Range(0, _buckets.Length)
                join bucket in 
                    from pair in pairs 
                    group pair by (uint)pair.Key.GetHashCode() % _divisor
                    on (uint)number equals bucket.Key into items
                from content in items.DefaultIfEmpty(Enumerable.Empty<KeyValuePair<TKey, TValue>>())
                select _buckets[number] = content.ToArray()).Count();
#pragma warning restore
        }

        [MethodImpl((MethodImplOptions)0x200)]
        public bool TryGet(TKey key, out TValue value)
        {
#pragma warning disable 8602
            var pairs = _buckets[key.GetHashCode() % _divisor];
#pragma warning restore 8602
            for (var index = 0; index < pairs.Length; index++)
            {
                var pair = pairs[index];
                if (Equals(pair.Key, key))
                {
                    value = pair.Value;
                    return true;
                }
            }

#pragma warning disable 8601
            value = default(TValue);
#pragma warning restore 8601
            return false;
        }
    }
}