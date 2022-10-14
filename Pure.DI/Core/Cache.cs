// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Cache<TKey, TValue> : ICache<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> _cache = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (!_cache.TryGetValue(key, out var value))
        {
            value = _cache[key] = valueFactory(key);
        }
        
        return value;
    }
}