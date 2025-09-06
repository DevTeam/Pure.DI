namespace Pure.DI.Core.Code;

record RootContext(
    DependencyGraph Graph,
    Root Root,
    IVarsMap VarsMap,
    Lines Lines)
{
    public bool IsThreadSafeEnabled => Graph.Source.Hints.IsThreadSafeEnabled;
}