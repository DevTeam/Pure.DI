// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class Arguments : IArguments
{
    public AttributeArgumentSyntax?[] GetArgs(
        AttributeArgumentListSyntax argumentListSyntax,
        params string[] colons) =>
        GetArgs(
            argumentListSyntax.Arguments.Count,
            i => argumentListSyntax.Arguments[i],
            arg => arg.NameColon?.Name.Identifier.Text,
            colons);

    public ArgumentSyntax?[] GetArgs(
        BaseArgumentListSyntax argumentListSyntax,
        params string[] colons) =>
        GetArgs(
            argumentListSyntax.Arguments.Count,
            i => argumentListSyntax.Arguments[i],
            arg => arg.NameColon?.Name.Identifier.Text,
            colons);

    public TypedConstant[] GetArgs(
        ImmutableArray<TypedConstant> attributeConstructorArguments,
        ImmutableArray<KeyValuePair<string, TypedConstant>> attributeNamedArguments,
        params string[] colons)
    {
        var size = colons.Length;
        if (size < attributeConstructorArguments.Length)
        {
            size = attributeConstructorArguments.Length;
        }

        var args = new TypedConstant[size];
        for (var argIndex = 0; argIndex < attributeConstructorArguments.Length; argIndex++)
        {
            var arg = attributeConstructorArguments[argIndex];
            args[argIndex] = arg;
        }

        for (var argIndex = 0; argIndex < colons.Length; argIndex++)
        {
            var col = colons[argIndex];
            foreach (var namedArgument in attributeNamedArguments.Where(namedArgument => namedArgument.Key == col))
            {
                args[argIndex] = namedArgument.Value;
                break;
            }
        }

        return args;
    }

    private static TArg?[] GetArgs<TArg>(
        int argsCount,
        Func<int, TArg> argProvider,
        Func<TArg, string?> nameProvider,
        string[] colons)
        where TArg : class
    {
        var args = new TArg?[colons.Length];
        for (var argIndex = 0; argIndex < argsCount; argIndex++)
        {
            var arg = argProvider(argIndex);
            if (nameProvider(arg) is { } colonName)
            {
                for (var colonIndex = 0; colonIndex < colons.Length; colonIndex++)
                {
                    if (colons[colonIndex] == colonName)
                    {
                        args[colonIndex] = arg;
                    }
                }

                continue;
            }

            if (argIndex < args.Length)
            {
                args[argIndex] = arg;
            }
        }

        return args;
    }
}
