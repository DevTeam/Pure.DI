namespace Pure.DI.Core;

interface IInstanceDpProvider
{
    InstanceDp Get(
        MdSetup setup,
        ITypeConstructor typeConstructor,
        INamedTypeSymbol implementationType);

    ImmutableArray<DpParameter> GetParameters(
        in MdSetup setup,
        in ImmutableArray<IParameterSymbol> parameters,
        ITypeConstructor typeConstructor);
}