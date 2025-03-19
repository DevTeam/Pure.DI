namespace Pure.DI.Core;

static class CollectionExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, [NotNullWhen(true)] out T? value)
    {
        if (stack.Count == 0)
        {
            value = default;
            return false;
        }

        value = stack.Pop()!;
        return true;
    }

    public static bool TryEnqueue<T>(this Queue<T> queue, [NotNullWhen(true)] out T? value)
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