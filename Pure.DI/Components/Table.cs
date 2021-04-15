// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable ArrangeDefaultValueWhenTypeNotEvident
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable MergeConditionalExpression
// ReSharper disable UnusedMember.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Pure.DI.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal class Table<TKey, TValue>
    {
        protected uint Divisor;
        protected KeyValuePair<TKey, TValue>[][] Buckets;

        public Table(ICollection<KeyValuePair<TKey, TValue>> pairs)
        {
            Divisor = (uint)pairs.Count + 1;
            Buckets = new KeyValuePair<TKey, TValue>[Divisor][];

#pragma warning disable CA1806
            (
                from number in Enumerable.Range(0, Buckets.Length)
                join bucket in 
                    from pair in pairs 
                    group pair by (uint)pair.Key.GetHashCode() % Divisor
                    on (uint)number equals bucket.Key into items
                from content in items.DefaultIfEmpty(Enumerable.Empty<KeyValuePair<TKey, TValue>>())
                select Buckets[number] = content.ToArray()).Count();
#pragma warning restore
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public TValue Get(TKey key)
        {
#pragma warning disable 8602
            var pairs = Buckets[key.GetHashCode() % Divisor];
#pragma warning restore 8602
            for (var index = 0; index < pairs.Length; index++)
            {
                var pair = pairs[index];
                if (Equals(pair.Key, key))
                {
                    return pair.Value;
                }
            }

#pragma warning disable 8603
            return default(TValue);
#pragma warning restore 8603
        }
    }

    internal class ResolversTable : Table<Type, Func<object>>
    {
        public ResolversTable(ICollection<KeyValuePair<Type, Func<object>>> pairs) : base(pairs) { }

        [MethodImpl((MethodImplOptions)0x100)]
        public new Func<object> Get(Type key)
        {
#pragma warning disable 8602
            var pairs = Buckets[key.GetHashCode() % Divisor];
#pragma warning restore 8602
            for (var index = 0; index < pairs.Length; index++)
            {
                var pair = pairs[index];
                if (pair.Key == key)
                {
                    return pair.Value;
                }
            }

#pragma warning disable 8603
            return default(Func<object>);
#pragma warning restore 8603
        }
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
            unchecked { _hashCode = (type.GetHashCode() * 397) ^ (tag != null ? tag.GetHashCode() : 0); }
        }

        public override bool Equals(object obj) =>
            obj is TagKey other
            && _type == other._type
            && Equals(_tag, other._tag);

        public override int GetHashCode() => _hashCode;
    }
}