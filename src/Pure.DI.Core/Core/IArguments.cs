namespace Pure.DI.Core;

interface IArguments
{
    AttributeArgumentSyntax?[] GetArgs(
        AttributeArgumentListSyntax argumentListSyntax,
        params string[] colons);

    ArgumentSyntax?[] GetArgs(
        BaseArgumentListSyntax argumentListSyntax,
        params string[] colons);

    TypedConstant[] GetArgs(
        ImmutableArray<TypedConstant> attributeConstructorArguments,
        ImmutableArray<KeyValuePair<string, TypedConstant>> attributeNamedArguments,
        params string[] colons);
}
