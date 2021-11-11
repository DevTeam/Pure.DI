// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Concurrent;

    internal class Cache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _cache = new();
        
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => _cache.GetOrAdd(key, valueFactory);
    }
}