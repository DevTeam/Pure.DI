namespace Pure.DI.Core.Code;

static class VarExtensions
{
    public static IEnumerable<VarDeclaration> GetArgsOfKind(this IEnumerable<VarDeclaration> args, ArgKind kind) =>
        args.Where(arg => arg.Node.Arg?.Source.Kind == kind);
}