// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable ArrangeDefaultValueWhenTypeNotEvident
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable MergeConditionalExpression
// ReSharper disable UnusedMember.Global
// ReSharper disable UseIndexFromEndExpression
// ReSharper disable ConvertToLambdaExpression
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UsePatternMatching
// ReSharper disable UseNullPropagation
// ReSharper disable InvertIf
#pragma warning disable 8618
#pragma warning disable 8604
#pragma warning disable 8603
#pragma warning disable 8602
#pragma warning disable 8625
#pragma warning disable 8765
#pragma warning disable 0436
namespace Pure.DI
{
    internal class Pair<TKey, TValue>
    {
        public readonly TKey Key;
        public readonly TValue Value;
        public Pair<TKey, TValue> Next;

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return "#" + Key + "=" + Value + "->" + Next;
        }
    }

    internal class Table<TKey, TValue>
    {
        protected readonly uint Divisor;
        protected readonly Pair<TKey, TValue>[] Buckets;

        public static uint GetDivisor(int count)
        {
            return (uint) (count + 1) * 4;
        }

        public Table(Pair<TKey, TValue>[] pairs, TKey defaultKey, TValue defaultValue)
        {
            Divisor = GetDivisor(pairs.Length);
            Buckets = new Pair<TKey, TValue>[Divisor];
            for (var i = 0; i < Buckets.Length; i++)
            {
                Buckets[i] = new Pair<TKey, TValue>(defaultKey, defaultValue);
            }

            var buckets = System.Linq.Enumerable.Select(System.Linq.Enumerable.GroupBy(pairs, pair => (uint) pair.Key.GetHashCode() % Divisor), groups => new {number = groups.Key, pairs = System.Linq.Enumerable.ToArray(groups)});

            foreach (var bucket in buckets)
            {
                Buckets[bucket.number] = bucket.pairs[0];
                for (var index = 1; index < bucket.pairs.Length; index++)
                {
                    bucket.pairs[index - 1].Next = bucket.pairs[index];
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]
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

    internal sealed class ResolversTable : Table<System.Type, System.Func<object>>
    {
        public readonly Pair<System.Type, System.Func<object>>[] ResolversBuckets;
        public readonly uint ResolversDivisor;

        public ResolversTable(Pair<System.Type, System.Func<object>>[] pairs)
            : base(pairs, typeof(ResolversTable), null)
        {
            ResolversBuckets = Buckets;
            ResolversDivisor = Divisor;
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        public object Resolve(System.Type type)
        {
            var pair = ResolversBuckets[(uint)type.GetHashCode() % ResolversDivisor];
            do
            {
                if (pair.Key == type)
                {
                    return pair.Value();
                }

                pair = pair.Next;
            } 
            while (pair != null);

            throw new System.ArgumentException("Cannot resolve an instance of the type " + type.Name + ".");
        }
    }

    internal sealed class ResolversByTagTable : Table<TagKey, System.Func<object>>
    {
        public readonly Pair<TagKey, System.Func<object>>[] ResolversByTagBuckets;
        public readonly uint ResolversByTagDivisor;
        public readonly Pair<System.Type, System.Func<object>>[] ResolversBuckets;
        public readonly uint ResolversDivisor;

        public ResolversByTagTable(ResolversTable resolversTable, Pair<TagKey, System.Func<object>>[] pairs)
            : base(pairs, new TagKey(typeof(ResolversByTagTable), null), null)
        {
            ResolversByTagBuckets = Buckets;
            ResolversByTagDivisor = Divisor;
            ResolversBuckets = resolversTable.ResolversBuckets;
            ResolversDivisor = resolversTable.ResolversDivisor;
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        public object Resolve(TagKey key)
        {
            var pair = ResolversByTagBuckets[(uint)key.GetHashCode() % ResolversByTagDivisor];
            do {
                // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                if (pair.Key.Equals(key))
                {
                    return pair.Value();
                }

                pair = pair.Next;
            }
            while (pair != null);

            if (key.Tag == null)
            {
                var keyType = key.Type;
                var typePair = ResolversBuckets[(uint)keyType.GetHashCode() % ResolversDivisor];
                do
                {
                    if (typePair.Key == keyType)
                    {
                        return typePair.Value();
                    }

                    typePair = typePair.Next;
                }
                while (typePair != null);
            }

            throw new System.ArgumentException("Cannot resolve an instance of the type " + key.Type.Name  + " with tag " + key.Tag + ".");
        }
    }

    internal struct TagKey
    {
        public readonly System.Type Type;
        public readonly object Tag;
        private readonly int _hashCode;

        public TagKey(System.Type type, object tag)
        {
            Type = type;
            Tag = tag;
            unchecked { _hashCode = (type.GetHashCode() * 397) ^ (tag != null ? tag.GetHashCode() : 0); }
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(TagKey other)
        {
            return Type == other.Type && Tag.Equals(other.Tag);
        }

        public override bool Equals(object obj)
        {
            var key = obj as TagKey?;
            return key != null && Equals(key);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}