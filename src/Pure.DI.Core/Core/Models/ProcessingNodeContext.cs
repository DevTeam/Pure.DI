namespace Pure.DI.Core.Models;

readonly record struct ProcessingNodeContext(
    ICache<ProcessingNodeKey, IProcessingNode> Cache,
    DependencyNode Node,
    object? ContextTag,
    ISet<Injection>? Contracts = null);