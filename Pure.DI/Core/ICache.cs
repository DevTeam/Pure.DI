namespace Pure.DI.Core;

internal interface ICache<TKey, TValue>
{
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);
}