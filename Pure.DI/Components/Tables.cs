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
// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantDelegateCreation
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable SuggestVarOrType_BuiltInTypes
#pragma warning disable 8618
#pragma warning disable 8604
#pragma warning disable 8603
#pragma warning disable 8602
#pragma warning disable 8601
#pragma warning disable 8625
#pragma warning disable 8765
#pragma warning disable 0436
namespace NS35EBD81B
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

    internal static class Tables
    {
        internal const string CannotResolveMessage = "Cannot resolve an instance ";
    } 

    internal class Table<TKey, TValue>
    {
        protected readonly uint Divisor;
        protected readonly Pair<TKey, TValue>[] Buckets;

        public static uint GetDivisor(int count)
        {
            return ((uint)count + 1) << 2;
        }

        public Table(Pair<TKey, TValue>[] pairs)
        {
            Divisor = GetDivisor(pairs.Length);
            Buckets = new Pair<TKey, TValue>[Divisor];
            for (var index = 0; index < pairs.Length; index++)
            {
                Pair<TKey, TValue> pair = pairs[index];
                uint bucket = (uint)pair.Key.GetHashCode() % Divisor;
                Pair<TKey, TValue> next = Buckets[bucket];
                Buckets[bucket] = pair;
                pair.Next = next;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]
        public TValue Get(TKey key)
        {
            Pair<TKey, TValue> pair = Buckets[(uint)key.GetHashCode() % Divisor];
            TKey pairKey;

            start:
            try
            {
                pairKey = pair.Key;
            }
            catch
            {
                return default(TValue);
            }

            if (Equals(pairKey, key))
            {
                return pair.Value;
            }

            pair = pair.Next;
            goto start;
        }
    }

    internal sealed class ResolversTable : Table<System.Type, System.Func<object>>
    {
        public readonly Pair<System.Type, System.Func<object>>[] ResolversBuckets;
        public readonly uint ResolversDivisor;

        public ResolversTable(Pair<System.Type, System.Func<object>>[] pairs)
            : base(pairs)
        {
            ResolversBuckets = Buckets;
            ResolversDivisor = Divisor;
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        public object Resolve(System.Type type)
        {
            Pair<System.Type, System.Func<object>> pair = ResolversBuckets[(uint)type.GetHashCode() % ResolversDivisor];
            System.Type pairKey;
            
            start:
            try
            {
                pairKey = pair.Key;
            }
            catch
            {
                throw new System.ArgumentException(Tables.CannotResolveMessage + type + ", consider adding it to the DI setup.");
            }
            
            if (pairKey == type)
            {
                return pair.Value();
            }

            pair = pair.Next;
            goto start;
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        internal System.Func<object> GetResolver<T>()
        {
            Pair<System.Type, System.Func<object>> pair = ResolversBuckets[(uint)typeof(T).GetHashCode() % ResolversDivisor];
            System.Type pairKey;
            
            start:
            try
            {
                pairKey = pair.Key;
            }
            catch
            {
                return new System.Func<object>(() =>
                {
                    throw new System.ArgumentException(Tables.CannotResolveMessage + typeof(T) + ", consider adding it to the DI setup.");
                });
            }

            if (pairKey == typeof(T))
            {
                return pair.Value;
            }

            pair = pair.Next;
            goto start;
        }
    }

    internal sealed class ResolversByTagTable : Table<TagKey, System.Func<object>>
    {
        public readonly Pair<TagKey, System.Func<object>>[] ResolversByTagBuckets;
        public readonly uint ResolversByTagDivisor;
        public readonly Pair<System.Type, System.Func<object>>[] ResolversBuckets;
        public readonly uint ResolversDivisor;

        public ResolversByTagTable(ResolversTable resolversTable, Pair<TagKey, System.Func<object>>[] pairs)
            : base(pairs)
        {
            ResolversByTagBuckets = Buckets;
            ResolversByTagDivisor = Divisor;
            ResolversBuckets = resolversTable.ResolversBuckets;
            ResolversDivisor = resolversTable.ResolversDivisor;
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x100)]
        public object Resolve(TagKey key)
        {
            Pair<TagKey, System.Func<object>> pair = ResolversByTagBuckets[key.HashCode % ResolversByTagDivisor];
            while (pair != null)
            {
                var pairKey = pair.Key;
                if (pairKey.Type == key.Type && (ReferenceEquals(pairKey.Tag, key.Tag) || Equals(pairKey.Tag, key.Tag)))
                {
                    return pair.Value();
                }

                pair = pair.Next;
            }

            if (key.Tag == null)
            {
                System.Type keyType = key.Type;
                Pair<System.Type, System.Func<object>> typePair = ResolversBuckets[(uint)keyType.GetHashCode() % ResolversDivisor];
                System.Type typePairKey;
                
                start:
                try
                {
                    typePairKey = typePair.Key;
                }
                catch
                {
                    throw new System.ArgumentException(Tables.CannotResolveMessage + key + ", consider adding it to the DI setup.");
                }
                
                if (typePairKey == keyType)
                {
                    return typePair.Value();
                }

                typePair = typePair.Next;
                goto start;
            }

            throw new System.ArgumentException(Tables.CannotResolveMessage + key + ", consider adding it to the DI setup.");
        }
    }

    internal struct TagKey
    {
        public readonly System.Type Type;
        public readonly object Tag;
        public readonly uint HashCode;

        public TagKey(System.Type type, object tag)
        {
            Type = type;
            Tag = tag;
            unchecked
            {
                HashCode = (uint)((type.GetHashCode() * 397) ^ (tag != null ? tag.GetHashCode() : 0));
            }
        }

        public override int GetHashCode()
        {
            return (int)HashCode;
        }

        public override string ToString()
        {
            return Type.Name + "(\"" + Tag + ")\"";
        }
    }
}