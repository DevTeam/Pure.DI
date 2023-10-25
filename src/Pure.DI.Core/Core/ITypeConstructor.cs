namespace Pure.DI.Core;

internal interface ITypeConstructor
{
    bool TryBind(ITypeSymbol source, ITypeSymbol target);

    ITypeSymbol Construct(Compilation compilation, ITypeSymbol type);
}