namespace Pure.DI.Core;

internal interface IUnboundTypeConstructor
{
    ITypeSymbol Construct(Compilation compilation, ITypeSymbol type);
}