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
// ReSharper disable UseIndexFromEndExpression
#pragma warning disable 8603
#pragma warning disable 8602
#pragma warning disable 8625
namespace Pure.DI.Components
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal class KeyValuePair<TKey, TValue>
    {
        public readonly TKey Key;
        public readonly TValue Value;
        public KeyValuePair<TKey, TValue> Next = null;

        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString() => $"#{Key}={Value} -> {Next}";
    }

    internal class Table<TKey, TValue>
    {
        protected uint Divisor;
        protected KeyValuePair<TKey, TValue>[] Buckets;

        public Table(KeyValuePair<TKey, TValue>[] pairs)
        {
            Divisor = (uint)pairs.Length + 1;
            Buckets = new KeyValuePair<TKey, TValue>[Divisor];
            for (var i = 0; i < Buckets.Length; i++)
            {
                Buckets[i] = null; // Empty
            }

            var buckets = (
                from pair in pairs
                group pair by (uint)pair.Key.GetHashCode() % Divisor into groups
                select new { number = groups.Key, pairs = groups.ToArray()});

            foreach (var bucket in buckets)
            {
                Buckets[bucket.number] = bucket.pairs[0];
                for (var index = 1; index < bucket.pairs.Length; index++)
                {
                    bucket.pairs[index - 1].Next = bucket.pairs[index];
                }
            }
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public TValue Get(TKey key)
        {
            var pair = Buckets[(uint)key.GetHashCode() % Divisor];
            while (pair != null)
            {
                if (Equals(pair.Key, key))
                {
                    return pair.Value;
                }

                pair = pair.Next;
            }

            return default(TValue);
        }
    }

    internal sealed class DependencyTable : Table<Type, Func<object>>
    {
        private Func<Type, object, object> _defaultFactory;
        public DependencyTable(KeyValuePair<Type, Func<object>>[] pairs, Func<Type, object, object> defaultFactory) : base(pairs)
        {
            _defaultFactory = defaultFactory;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public object Resolve(Type key)
        {
            var pair = Buckets[(uint)key.GetHashCode() % Divisor];
            while (pair != null)
            {
                if (pair.Key == key)
                {
                    return pair.Value();
                }

                pair = pair.Next;
            }

            return _defaultFactory !=null ? 
                _defaultFactory(key, null) ?? new ArgumentException($"Cannot resolve an instance of the type {key.Name}.")
                : throw new ArgumentException($"Cannot resolve an instance of the type {key.Name}.");
        }
    }

    internal sealed class TagDependencyTable : Table<TagKey, Func<object>>
    {
        private Func<Type, object, object> _defaultFactory;
        public TagDependencyTable(KeyValuePair<TagKey, Func<object>>[] pairs, Func<Type, object, object> defaultFactory) : base(pairs)
        {
            _defaultFactory = defaultFactory;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public object Resolve(TagKey key)
        {
            var pair = Buckets[(uint)key.GetHashCode() % Divisor];
            while (pair != null)
            {
                if (pair.Key.Equals(key))
                {
                    return pair.Value();
                }

                pair = pair.Next;
            }

            return _defaultFactory != null ?
                _defaultFactory(key.Type, key.Tag) ?? new ArgumentException($"Cannot resolve an instance of the type {key.Type.Name} with tag {key.Tag}.")
                : throw new ArgumentException($"Cannot resolve an instance of the type {key.Type.Name} with tag {key.Tag}.");
        }
    }

    internal struct TagKey
    {
        public readonly Type Type;
        public readonly object Tag;
        private readonly int _hashCode;

        public TagKey(Type type, object tag)
        {
            Type = type;
            Tag = tag;
            unchecked { _hashCode = (type.GetHashCode() * 397) ^ (tag != null ? tag.GetHashCode() : 0); }
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public bool Equals(ref TagKey other) => Type == other.Type && Tag.Equals(other.Tag);

        public override bool Equals(object obj) => obj is TagKey other && Equals(ref other);

        public override int GetHashCode() => _hashCode;
    }
}