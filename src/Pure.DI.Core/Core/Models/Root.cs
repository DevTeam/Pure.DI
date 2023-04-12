namespace Pure.DI.Core.Models;

internal record Root(
    int Index,
    DependencyNode Node,
    in Injection Injection,
    string Name,
    in ImmutableArray<Line> Lines)
{
    public bool IsPublic => !string.IsNullOrWhiteSpace(Name);

    public string PropertyName => IsPublic ? Name : $"Root{Index.ToString()}Prop{Variable.Postfix}";
}