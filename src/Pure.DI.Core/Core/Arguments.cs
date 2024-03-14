// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Arguments : IArguments
{
    public ArgumentSyntax?[] GetArgs(BaseArgumentListSyntax argumentListSyntax, params string[] colons)
    {
        var args = new ArgumentSyntax[colons.Length];
        for (var argIndex = 0; argIndex < argumentListSyntax.Arguments.Count; argIndex++)
        {
            var arg = argumentListSyntax.Arguments[argIndex];
            if (arg.NameColon?.Name.Identifier.Text is { } colonName)
            {
                for (var colonIndex = 0; colonIndex < colons.Length; colonIndex++)
                {
                    if (colons[colonIndex] == colonName)
                    {
                        args[colonIndex] = arg;
                    }
                }
            }
            else
            {
                if (argIndex < args.Length)
                {
                    args[argIndex] = arg;
                }
            }
        }

        return args;
    }
}