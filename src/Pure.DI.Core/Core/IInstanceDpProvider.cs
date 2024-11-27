namespace Pure.DI.Core;

internal interface IInstanceDpProvider
{
    InstanceDp Get(
        MdSetup setup,
        ITypeConstructor typeConstructor,
        Compilation compilation,
        INamedTypeSymbol implementationType);

    ImmutableArray<DpParameter> GetParameters(
        in MdSetup setup,
        in ImmutableArray<IParameterSymbol> parameters,
        Compilation compilation,
        ITypeConstructor typeConstructor);
}