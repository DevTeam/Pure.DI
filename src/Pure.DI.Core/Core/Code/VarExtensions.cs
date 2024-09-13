namespace Pure.DI.Core.Code;

internal static class VarExtensions
{
    public static IEnumerable<IStatement> GetPath(this IStatement statement)
    {
        var parent = statement;
        while (parent is not null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public static IEnumerable<Variable> GetArgsOfKind(this IEnumerable<Variable> args, ArgKind kind) =>
        args.Where(arg => arg.Node.Arg?.Source.Kind == kind);
}