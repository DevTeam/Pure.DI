namespace Pure.DI.Core.Code;

internal interface ITypeResolver
{
    TypeDescription Resolve(MdSetup setup, ITypeSymbol type);
}