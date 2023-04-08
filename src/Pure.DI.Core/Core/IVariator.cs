namespace Pure.DI.Core;

// ReSharper disable once IdentifierTypo
internal interface IVariator<T>
{
    bool TryGetNextVariants(
        IEnumerable<IEnumerator<T>> variations,
        Predicate<T> hasVariantsPredicate,
        [NotNullWhen(true)] out IReadOnlyCollection<T>? variants);
}