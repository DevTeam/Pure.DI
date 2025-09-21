// ReSharper disable UnusedParameter.Global

namespace Pure.DI.Core;

interface IGraphVisitor<in TContext, T>
{
    T Create(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        T? parent = default);

    T AppendDependency(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        Dependency dependency,
        T? parent = default);

    bool Visit(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        in T element);
}