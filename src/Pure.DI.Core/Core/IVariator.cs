namespace Pure.DI.Core;

// ReSharper disable once IdentifierTypo
interface IVariator<T>
{
    bool TryGetNext(IEnumerable<ISetOfOptions<T>> setsOfOptions, out ImmutableArray<T> options);
}