// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Core;

interface IGraphVisitor<in TContext, T>
{
    T Create(
        TContext ctx,
        DependencyGraph dependencyGraph,
        DependencyNode rootNode,
        T? parent = default);

    T AppendDependency(
        TContext ctx,
        DependencyGraph dependencyGraph,
        Dependency dependency,
        T? parent = default);

    bool Visit(
        TContext ctx,
        DependencyGraph dependencyGraph,
        in T element);
}