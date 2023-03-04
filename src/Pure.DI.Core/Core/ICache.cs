namespace Pure.DI.Core;

internal interface ICache<TKey, out TValue>
{
    TValue Get(in TKey key);
}