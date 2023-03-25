namespace Pure.DI.Core.Models;

internal record Root(
    int Index,
    DependencyNode Node,
    in Injection Injection,
    bool IsPublic,
    string Name,
    in ImmutableArray<Line> Lines)
{
    public string PropertyName => IsPublic ? Name : $"Root{Index}{Variable.Postfix}";
};