namespace Pure.DI.Core;

interface IOverridesRegistry
{
    void Register(DependencyNode rootNode, DpOverride @override);

    IEnumerable<DpOverride> GetOverrides(DependencyNode rootNode);
}