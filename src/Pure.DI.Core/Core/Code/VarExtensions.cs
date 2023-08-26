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
}