namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class CollectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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