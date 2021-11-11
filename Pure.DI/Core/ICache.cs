namespace Pure.DI.Core
{
    using System;

    internal interface ICache<TKey, TValue>
    {
        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);
    }
}