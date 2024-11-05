// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo

namespace Pure.DI.Core;

internal sealed class Variator<T> : IVariator<T>
    where T: class
{
    public bool TryGetNextVariants(
        IEnumerable<IEnumerator<T>> variations,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? variants)
    {
        var hasNext = false;
        var curVariants = new List<T>();
        foreach (var enumerator in variations)
        {
            if (enumerator.Current is null)
            {
                enumerator.MoveNext();
                var current = enumerator.Current;
                if (current is not null)
                {
                    curVariants.Add(current);
                    hasNext = true;
                }

                continue;
            }

            if (!hasNext)
            {
                hasNext = enumerator.MoveNext();
            }

            curVariants.Add(enumerator.Current);
        }

        if (hasNext)
        {
            variants = curVariants;
            return hasNext;
        }

        variants = default;
        return false;
    }
}