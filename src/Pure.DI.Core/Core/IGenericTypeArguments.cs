namespace Pure.DI.Core;

interface IGenericTypeArguments
{
    bool IsGenericTypeArgument(MdSetup setup, ITypeSymbol typeSymbol);

    bool IsGenericTypeArgumentAttribute(MdSetup setup, ITypeSymbol typeSymbol);
}