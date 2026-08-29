namespace Pure.DI.Core;

interface ICache<TKey, TValue>
{
    bool TryGet(in TKey key, out TValue value);

    TValue Get(in TKey key, Func<TKey, TValue> factory);

    void Set(in TKey key, in TValue value);

    void Remove(in TKey key);
}
