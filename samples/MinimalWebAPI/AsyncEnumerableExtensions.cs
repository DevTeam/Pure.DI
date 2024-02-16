namespace MinimalWebAPI;

internal static class AsyncEnumerableExtensions
{
    public static async Task<IReadOnlyCollection<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }
}