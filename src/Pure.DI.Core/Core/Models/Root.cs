namespace Pure.DI.Core.Models;

internal record Root(
    DependencyNode Node,
    in Injection Injection,
    bool IsPublic,
    string PropertyName);