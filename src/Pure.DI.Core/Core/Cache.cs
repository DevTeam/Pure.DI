// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Cache<TKey, TValue>(
    IEqualityComparer<TKey> comparer)
    : ICache<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> _dictionary = new(comparer);

    public TValue Get(in TKey key, Func<TKey, TValue> factory)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        value = factory(key);
        _dictionary[key] = value;
        return value;
    }

    public void Set(in TKey key, in TValue value) => _dictionary[key] = value;
}