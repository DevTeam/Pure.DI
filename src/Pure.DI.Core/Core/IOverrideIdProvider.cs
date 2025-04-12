namespace Pure.DI.Core;

interface IOverrideIdProvider
{
    int GetId(ITypeSymbol type, in ImmutableArray<MdTag> tags);
}