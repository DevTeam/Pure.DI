namespace Pure.DI.Core;

interface ILifetimeOptimizer
{
    Lifetime Optimize(Root root, DependencyGraph graph, IDependencyNode node, StringBuilder trace);
}