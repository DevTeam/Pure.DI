// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal sealed class Cache<TKey, TValue> : ICache<TKey, TValue>
{
    private readonly Func<TKey, TValue> _factory;
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

    public Cache(
        Func<TKey, TValue> factory,
        IEqualityComparer<TKey> comparer)
    {
        _factory = factory;
        _dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
    }

    public TValue Get(in TKey key) => _dictionary.GetOrAdd(key, _factory);

    public void Set(in TKey key, in TValue value) => _dictionary[key] = value;
}