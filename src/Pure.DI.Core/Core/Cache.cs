// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal sealed class Cache<TKey, TValue>(
    Func<TKey, TValue> factory,
    IEqualityComparer<TKey> comparer)
    : ICache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new(comparer);

    public TValue Get(in TKey key) => _dictionary.GetOrAdd(key, factory);

    public void Set(in TKey key, in TValue value) => _dictionary[key] = value;
}