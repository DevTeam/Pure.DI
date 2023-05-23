namespace Pure.DI.Core;

internal static class QueueExtensions
{
    public static bool TryDequeue<T>(this Queue<T> queue, [NotNullWhen(true)] out T? value)
    {
        if (queue.Count == 0)
        {
            value = default;
            return false;
        }

        value = queue.Dequeue()!;
        return true;
    }
}