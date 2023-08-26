namespace Pure.DI.Core.Code;

internal interface IVariablesBuilder
{
    Block Build(
        IGraph<DependencyNode, Dependency> graph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection);
}