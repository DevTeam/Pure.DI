namespace Pure.DI.Core;

class LifetimeProvider(ITypes types, IBaseSymbolsProvider baseSymbolsProvider) : ILifetimeProvider
{
    public MdLifetime? GetActualLifetime(
        IReadOnlyCollection<MdDefaultLifetime> defaultLifetimes,
        MdLifetime? lifetime,
        ITypeSymbol? type,
        IReadOnlyCollection<MdTag> tags,
        IReadOnlyCollection<MdContract> contracts)
    {
        if (lifetime is not null)
        {
            return lifetime;
        }

        if (type is null)
        {
            return GetDefaultLifetime(defaultLifetimes);
        }

        foreach (var defaultLifetime in defaultLifetimes.Where(i => i.Type is not null).Reverse())
        {
            var baseSymbols = baseSymbolsProvider.GetBaseSymbols(type, (baseType, _) =>
                IsMatchingDefaultLifetime(defaultLifetime, baseType, tags, contracts));

            if (baseSymbols.Any())
            {
                return defaultLifetime.Lifetime;
            }
        }

        return GetDefaultLifetime(defaultLifetimes);
    }

    private static MdLifetime GetDefaultLifetime(IReadOnlyCollection<MdDefaultLifetime> defaultLifetimes) =>
        defaultLifetimes.FirstOrDefault(i => i.Type is null).Lifetime;

    private bool IsMatchingDefaultLifetime(
        MdDefaultLifetime defaultLifetime,
        ITypeSymbol type,
        IReadOnlyCollection<MdTag> implementationTags,
        IReadOnlyCollection<MdContract> contracts)
    {
        if (!types.TypeEquals(defaultLifetime.Type, type))
        {
            return false;
        }

        if (defaultLifetime.Tags.IsDefaultOrEmpty)
        {
            return true;
        }

        // Combine implementation tags and contract tags for intersection check
        var contractTags = contracts.FirstOrDefault(j => types.TypeEquals(j.ContractType, type)).Tags;
        var combinedTags = implementationTags.ToImmutableHashSet();
        if (!contractTags.IsDefaultOrEmpty)
        {
            combinedTags = combinedTags.Union(contractTags);
        }

        return !combinedTags.Intersect(defaultLifetime.Tags).IsEmpty;
    }
}