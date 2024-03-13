namespace Pure.DI.Core.Models;

internal record Root(
    int Index,
    DependencyNode Node,
    MdRoot Source,
    in Injection Injection,
    string Name,
    in ImmutableArray<Line> Lines,
    in ImmutableArray<Variable> Args,
    RootKinds Kind,
    TypeDescription TypeDescription = default,
    bool IsMethod = false)
{
    public bool IsPublic => !string.IsNullOrWhiteSpace(Name);

    public string DisplayName => this.GetPropertyName();
}