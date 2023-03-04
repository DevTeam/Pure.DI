namespace Pure.DI.Core;

internal interface ITypeConstructor
{
    void Bind(ITypeSymbol source, ITypeSymbol target);

    ITypeSymbol Construct(Compilation compilation, ITypeSymbol type);
}