namespace Pure.DI.Core;

interface ITypeConstructor
{
    bool TryBind(MdSetup setup, ITypeSymbol source, ITypeSymbol target);

    ITypeSymbol Construct(MdSetup setup, ITypeSymbol type);

    ITypeSymbol ConstructReversed(ITypeSymbol type);
}