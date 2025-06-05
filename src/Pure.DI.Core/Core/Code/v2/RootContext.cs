namespace Pure.DI.Core.Code.v2;

record RootContext(
    DependencyGraph Graph,
    Root Root,
    IVarsMap VarsMap,
    LinesBuilder Lines)
{
    public bool IsThreadSafeEnabled => Graph.Source.Hints.IsThreadSafeEnabled;
}