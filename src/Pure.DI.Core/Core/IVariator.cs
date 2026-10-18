namespace Pure.DI.Core;

// ReSharper disable once IdentifierTypo
interface IVariator<T>
{
    bool TryGetNext(IEnumerable<ISetOfOptions<T>> setsOfOptions, out ImmutableArray<T> options);

    bool TryGetNext(
        IEnumerable<ISetOfOptions<T>> setsOfOptions,
        IReadOnlyCollection<ISetOfOptions<T>> prioritizedSetsOfOptions,
        out ImmutableArray<T> options);
}
