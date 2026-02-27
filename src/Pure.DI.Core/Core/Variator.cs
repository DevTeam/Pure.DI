// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo

// ReSharper disable InvertIf
namespace Pure.DI.Core;

sealed class Variator<T> : IVariator<T>
{
    public bool TryGetNext(IEnumerable<ISetOfOptions<T>> setsOfOptions, out ImmutableArray<T> options)
    {
        var enumerators = setsOfOptions.ToList();
        if (enumerators.Count == 0)
        {
            options = default;
            return false;
        }

        if (enumerators.All(v => !v.IsStarted))
        {
            var initial = ImmutableArray.CreateBuilder<T>(enumerators.Count);
            foreach (var enumerator in enumerators)
            {
                if (!TryMoveToNext(enumerator))
                {
                    options = default;
                    return false;
                }

                initial.Add(enumerator.Current!);
            }

            options = initial.MoveToImmutable();
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
                        options = default;
                        return false;
                    }
                }

                options = enumerators.Select(v => v.Current!).ToImmutableArray();
                return true;
            }

            // MoveNext failed - this enumerator is exhausted
            // If this is the last enumerator, all combinations are exhausted
            if (index == enumerators.Count - 1)
            {
                options = default;
                return false;
            }

            // Not the last enumerator - reset this one and continue to next
            enumerator.Reset();
            if (!TryMoveToNext(enumerator))
            {
                options = default;
                return false;
            }
        }

        options = default;
        return false;
    }

    private static bool TryMoveToNext(ISetOfOptions<T> enumerator) =>
        enumerator.MoveNext() && enumerator.Current is not null;
}
