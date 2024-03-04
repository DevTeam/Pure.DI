namespace Pure.DI.Core;

internal interface ICache<TKey, TValue>
{
    TValue Get(in TKey key, Func<TKey, TValue> factory);
    
    void Set(in TKey key, in TValue value);
}