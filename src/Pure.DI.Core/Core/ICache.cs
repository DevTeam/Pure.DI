namespace Pure.DI.Core;

internal interface ICache<TKey, TValue>
{
    TValue Get(in TKey key);
    
    void Set(in TKey key, in TValue value);
}