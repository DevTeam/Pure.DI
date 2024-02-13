namespace Pure.DI.Core;

internal interface IArguments
{
    ArgumentSyntax?[] GetArgs(BaseArgumentListSyntax argumentListSyntax, params string[] colons);
}