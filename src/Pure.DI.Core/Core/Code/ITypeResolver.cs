namespace Pure.DI.Core.Code;

interface ITypeResolver
{
    TypeDescription Resolve(MdSetup setup, ITypeSymbol type);
}