// ReSharper disable UnusedParameter.Global
namespace Pure.DI.Core;

internal interface IGraphVisitor<in TContext, T>
{
    T Create(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode currentNode,
        T? parent = default);
    
    bool Visit(
        TContext ctx,
        IGraph<DependencyNode, Dependency> graph,
        in T element);
}