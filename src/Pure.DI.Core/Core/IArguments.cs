namespace Pure.DI.Core;

internal interface IArguments
{
    ArgumentSyntax?[] GetArgs(
        BaseArgumentListSyntax argumentListSyntax,
        params string[] colons);
    
    TypedConstant[] GetArgs(
        ImmutableArray<TypedConstant> attributeConstructorArguments,
        ImmutableArray<KeyValuePair<string, TypedConstant>> attributeNamedArguments,
        params string[] colons);
}