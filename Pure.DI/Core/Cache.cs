// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Collections.Generic;

    internal class Cache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _cache = new();
        
        public bool TryGetValue(TKey key, out TValue value) => _cache.TryGetValue(key, out value);

        public void Add(TKey key, TValue value) => _cache.Add(key, value);
    }
}