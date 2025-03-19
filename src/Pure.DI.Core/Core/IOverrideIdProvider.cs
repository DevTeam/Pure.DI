namespace Pure.DI.Core;

interface IOverrideIdProvider
{
    int GetId(ITypeSymbol type, ISet<object> tags);
}