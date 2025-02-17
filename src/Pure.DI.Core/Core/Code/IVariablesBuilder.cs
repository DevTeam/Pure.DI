namespace Pure.DI.Core.Code;

interface IVariablesBuilder
{
    Block Build(
        DependencyGraph dependencyGraph,
        IDictionary<MdBinding, Variable> map,
        DependencyNode rootNode,
        Injection rootInjection);
}