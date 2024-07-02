namespace Pure.DI.Core.Code;

internal interface IVariablesBuilder
{
    Block Build(
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection);
}