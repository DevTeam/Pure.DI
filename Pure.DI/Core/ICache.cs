namespace Pure.DI.Core
{
    internal interface ICache<in TKey, TValue>
    {
        bool TryGetValue(TKey key, out TValue value);

        void Add(TKey key, TValue value);
    }
}