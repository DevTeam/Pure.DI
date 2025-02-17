namespace Pure.DI.Core;

interface ITypeConstructor
{
    bool TryBind(MdSetup setup, ITypeSymbol source, ITypeSymbol target);

    ITypeSymbol Construct(MdSetup setup, Compilation compilation, ITypeSymbol type);

    ITypeSymbol ConstructReversed(ITypeSymbol type);
}