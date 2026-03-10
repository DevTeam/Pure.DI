namespace Pure.DI.Core.Models;

readonly record struct ProcessingNodeContext(
    DependencyNode Node,
    object? ContextTag,
    ISet<Injection>? Contracts = null);