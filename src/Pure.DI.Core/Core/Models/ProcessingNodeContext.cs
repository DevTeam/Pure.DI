namespace Pure.DI.Core.Models;

record ProcessingNodeContext(
    ICache<ProcessingNodeKey, IProcessingNode> Cache,
    DependencyNode Node,
    object? ContextTag,
    ISet<Injection>? Contracts = null);