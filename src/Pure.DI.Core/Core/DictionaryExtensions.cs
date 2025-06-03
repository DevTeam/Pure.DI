namespace Pure.DI.Core;

static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        var result = new Dictionary<TKey, TValue>(
            dictionary.Count,
            dictionary is Dictionary<TKey, TValue> dict ? dict.Comparer : EqualityComparer<TKey>.Default);

        foreach (var item in dictionary)
        {
            result.Add(item.Key, item.Value);
        }

        return result;
    }
}