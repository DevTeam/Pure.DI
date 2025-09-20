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
}