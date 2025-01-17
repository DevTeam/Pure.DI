namespace Pure.DI.Core;

internal interface IGenericTypeArguments
{
    bool IsGenericTypeArgument(MdSetup setup, ITypeSymbol typeSymbol);
    bool IsGenericTypeArgumentAttribute(MdSetup setup, ITypeSymbol typeSymbol);
}