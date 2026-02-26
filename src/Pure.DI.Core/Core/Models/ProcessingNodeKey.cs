namespace Pure.DI.Core.Models;

record struct ProcessingNodeKey(
    int Variation,
    DependencyNode Node,
    object? ContextTag)
{
    public ISet<Injection>? Contracts;
};