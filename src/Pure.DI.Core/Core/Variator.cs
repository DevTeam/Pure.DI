// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

internal sealed class Variator<T> : IVariator<T>
{
    public bool TryGetNextVariants(
        IEnumerable<IEnumerator<T>> variations,
        Predicate<T> hasVariantsPredicate,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? variants)
    {
        var hasNext = false;
        var curVariants = new List<T>();
        foreach (var enumerator in variations)
        {
            if (!hasNext && enumerator.MoveNext())
            {
                hasNext = true;
                curVariants.Add(enumerator.Current);
                continue;
            }

            if (hasVariantsPredicate(enumerator.Current))
            {
                enumerator.MoveNext();
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