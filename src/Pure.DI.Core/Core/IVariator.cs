namespace Pure.DI.Core;

// ReSharper disable once IdentifierTypo
interface IVariator<T>
{
    bool TryGetNext(
        IEnumerable<ISetOfOptions<T>> setsOfOptions,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? options);
}