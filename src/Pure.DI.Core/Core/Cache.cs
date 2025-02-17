// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Collections.Concurrent;

sealed class Cache<TKey, TValue>(
    IEqualityComparer<TKey> comparer)
    : ICache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new(comparer);

    public TValue Get(in TKey key, Func<TKey, TValue> factory) => _dictionary.GetOrAdd(key, factory);

    public void Set(in TKey key, in TValue value) => _dictionary[key] = value;
}