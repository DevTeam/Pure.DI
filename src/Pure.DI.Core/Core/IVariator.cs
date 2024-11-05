namespace Pure.DI.Core;

// ReSharper disable once IdentifierTypo
internal interface IVariator<T>
    where T: class
{
    bool TryGetNextVariants(
        IEnumerable<IEnumerator<T>> variations,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? variants);
}