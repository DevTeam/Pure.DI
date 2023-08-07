namespace Pure.DI.Core.Models;

internal record Root(
    int Index,
    DependencyNode Node,
    in Injection Injection,
    string Name,
    in ImmutableArray<Line> Lines,
    in ImmutableArray<Variable> Args)
{
    public bool IsPublic => !string.IsNullOrWhiteSpace(Name);

    public string PropertyName => IsPublic ? Name : $"Root{Variable.Salt}_{Index:0000}";
}