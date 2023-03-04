// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal class Cache<TKey, TValue> : ICache<TKey, TValue>
{
    private readonly Func<TKey, TValue> _factory;
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();

    public Cache(Func<TKey, TValue> factory) => _factory = factory;

    public TValue Get(in TKey key) => _dictionary.GetOrAdd(key, _factory);
}