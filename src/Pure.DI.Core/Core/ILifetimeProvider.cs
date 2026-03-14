namespace Pure.DI.Core;

interface ILifetimeProvider
{
    MdLifetime? GetActualLifetime(
        IReadOnlyCollection<MdDefaultLifetime> defaultLifetimes,
        MdLifetime? lifetime,
        ITypeSymbol? type,
        IReadOnlyCollection<MdTag> tags,
        IReadOnlyCollection<MdContract> contracts,
        bool useCommonDefault);
}