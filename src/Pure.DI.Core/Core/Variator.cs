// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo

// ReSharper disable InvertIf
namespace Pure.DI.Core;

sealed class Variator<T> : IVariator<T>
{
    public bool TryGetNext(
        IEnumerable<ISetOfOptions<T>> setsOfOptions,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? options)
    {
        var enumerators = setsOfOptions.ToList();
        if (enumerators.Count == 0)
        {
            options = null;
            return false;
        }

        if (enumerators.All(v => !v.IsStarted))
        {
            var initial = new List<T>(enumerators.Count);
            foreach (var enumerator in enumerators)
            {
                if (!TryMoveToNext(enumerator))
                {
                    options = null;
                    return false;
                }

                initial.Add(enumerator.Current!);
            }

            options = initial;
            return true;
        }

        for (var index = 0; index < enumerators.Count; index++)
        {
            var enumerator = enumerators[index];
            if (TryMoveToNext(enumerator))
            {
                for (var resetIndex = 0; resetIndex < index; resetIndex++)
                {
                    var resetEnumerator = enumerators[resetIndex];
                    resetEnumerator.Reset();
                    if (!TryMoveToNext(resetEnumerator))
                    {
                        options = null;
                        return false;
                    }
                }

                options = enumerators.Select(v => v.Current!).ToList();
                return true;
            }

            // MoveNext failed - this enumerator is exhausted
            // If this is the last enumerator, all combinations are exhausted
            if (index == enumerators.Count - 1)
            {
                options = null;
                return false;
            }

            // Not the last enumerator - reset this one and continue to next
            enumerator.Reset();
            if (!TryMoveToNext(enumerator))
            {
                options = null;
                return false;
            }
        }

        options = null;
        return false;
    }

    private static bool TryMoveToNext(ISetOfOptions<T> enumerator) =>
        enumerator.MoveNext() && enumerator.Current is not null;
}
